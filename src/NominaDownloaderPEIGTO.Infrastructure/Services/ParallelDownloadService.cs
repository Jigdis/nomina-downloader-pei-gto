using NominaDownloaderPEIGTO.Application.Interfaces;
using NominaDownloaderPEIGTO.Domain.Entities;
using NominaDownloaderPEIGTO.Domain.ValueObjects;

namespace NominaDownloaderPEIGTO.Infrastructure.Services
{
    /// <summary>
    /// Servicio para manejar descargas paralelas con múltiples navegadores
    /// </summary>
    public class ParallelDownloadService : IParallelDownloadService
    {
        private readonly IDownloadSessionRepository _sessionRepository;
        private readonly IProgressService _progressService;
        private readonly SemaphoreSlim _semaphore;

        public ParallelDownloadService(
            IDownloadSessionRepository sessionRepository,
            IProgressService progressService)
        {
            _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
            _progressService = progressService ?? throw new ArgumentNullException(nameof(progressService));
            _semaphore = new SemaphoreSlim(16, 16); // Máximo 16 navegadores paralelos
        }

        public async Task ProcessSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);
            if (session == null)
            {
                throw new InvalidOperationException($"Sesión {sessionId} no encontrada");
            }

            try
            {
                // Procesar todas las tareas en paralelo
                var tasks = session.PeriodTasks.Select(periodTask => 
                    ProcessTaskWithRetryAsync(session, periodTask, cancellationToken));

                await Task.WhenAll(tasks);

                // Completar sesión
                session.Complete();
                await _sessionRepository.UpdateAsync(session, cancellationToken);
                await _progressService.NotifySessionCompletedAsync(session, cancellationToken);
            }
            catch (Exception ex)
            {
                session.Fail(ex.Message);
                await _sessionRepository.UpdateAsync(session, cancellationToken);
                await _progressService.NotifySessionCompletedAsync(session, cancellationToken);
                throw;
            }
        }

        private async Task ProcessTaskWithRetryAsync(
            DownloadSession session, 
            PeriodTask task, 
            CancellationToken cancellationToken)
        {
            var maxRetries = session.Config.MaxRetryAttempts;
            
            while (task.CanRetry(maxRetries) && !cancellationToken.IsCancellationRequested)
            {
                await _semaphore.WaitAsync(cancellationToken);
                
                try
                {
                    await ProcessSingleTaskAsync(session, task, cancellationToken);
                    return; // Éxito, salir del bucle de reintentos
                }
                catch (Exception ex)
                {
                    task.Fail(ex.Message);
                    
                    var failedTask = new FailedTask(
                        task.Period, 
                        ex.Message, 
                        task.AttemptCount,
                        ex.StackTrace,
                        "Chrome Browser");
                    
                    session.AddFailedTask(failedTask);
                    await _progressService.NotifyTaskFailedAsync(failedTask, cancellationToken);
                    
                    if (!task.CanRetry(maxRetries))
                    {
                        // Se agotaron los reintentos
                        await _sessionRepository.UpdateAsync(session, cancellationToken);
                        return;
                    }
                    
                    // Esperar antes del siguiente intento
                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                    task.Reset(); // Preparar para el siguiente intento
                }
                finally
                {
                    _semaphore.Release();
                }
            }
        }

        private async Task ProcessSingleTaskAsync(
            DownloadSession session, 
            PeriodTask task, 
            CancellationToken cancellationToken)
        {
            using var webPortalService = new SeleniumWebPortalService(session.Config);
            var fileValidationService = new FileValidationService();

            // Iniciar tarea
            task.Start();
            await _progressService.NotifyTaskStartedAsync(task, cancellationToken);

            // Login
            var loginSuccess = await webPortalService.LoginAsync(session.Credentials, cancellationToken);
            if (!loginSuccess)
            {
                throw new InvalidOperationException("Error en el login");
            }

            // Descargar archivo
            var fileMetadata = await webPortalService.DownloadFileAsync(
                task.Period, 
                session.Config.DownloadPath, 
                cancellationToken);

            // Validar archivo si está habilitado
            if (session.Config.ValidateDownloads)
            {
                fileMetadata = await fileValidationService.ValidateFileAsync(
                    fileMetadata.FilePath, 
                    cancellationToken);
            }

            // Crear archivo descargado
            var downloadedFile = new DownloadedFile(task.Period, fileMetadata);
            
            if (fileMetadata.IsValid)
            {
                downloadedFile.MarkAsValid();
            }
            else
            {
                downloadedFile.MarkAsInvalid("Validación falló");
            }

            // Agregar archivo a la tarea y sesión
            task.AddFile(downloadedFile);
            session.AddDownloadedFile(downloadedFile);

            // Completar tarea
            task.Complete();
            await _progressService.NotifyTaskCompletedAsync(task, cancellationToken);
            await _progressService.NotifyFileDownloadedAsync(downloadedFile, cancellationToken);

            // Logout
            await webPortalService.LogoutAsync(cancellationToken);

            // Actualizar sesión
            await _sessionRepository.UpdateAsync(session, cancellationToken);
        }

        public void Dispose()
        {
            _semaphore?.Dispose();
        }
    }
}
