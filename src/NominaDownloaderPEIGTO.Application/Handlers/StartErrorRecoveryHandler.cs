using NominaDownloaderPEIGTO.Application.Commands;
using NominaDownloaderPEIGTO.Application.Interfaces;

namespace NominaDownloaderPEIGTO.Application.Handlers
{
    /// <summary>
    /// Handler para el comando de recuperación de errores
    /// </summary>
    public class StartErrorRecoveryHandler
    {
        private readonly IErrorRecoveryService _recoveryService;
        private readonly IWebPortalService _webPortalService;
        private readonly IProgressService _progressService;

        public StartErrorRecoveryHandler(
            IErrorRecoveryService recoveryService,
            IWebPortalService webPortalService,
            IProgressService progressService)
        {
            _recoveryService = recoveryService ?? throw new ArgumentNullException(nameof(recoveryService));
            _webPortalService = webPortalService ?? throw new ArgumentNullException(nameof(webPortalService));
            _progressService = progressService ?? throw new ArgumentNullException(nameof(progressService));
        }

        public async Task<StartErrorRecoveryResult> Handle(
            StartErrorRecoveryCommand command, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Crear sesión de recuperación
                var recoverySession = await _recoveryService.CreateRecoverySessionAsync(
                    command.OriginalSessionId,
                    command.FailedPeriods,
                    cancellationToken);

                // Configurar intentos máximos
                var maxRetries = command.MaxRetryAttempts;

                // Limpiar carpetas fallidas
                await _recoveryService.CleanupFailedFoldersAsync(
                    command.FailedPeriods,
                    command.DownloadPath,
                    cancellationToken);

                // Iniciar recuperación
                recoverySession.StartRecovery();
                
                var successfulPeriods = new List<Domain.ValueObjects.PeriodInfo>();
                var stillFailedPeriods = new List<Domain.ValueObjects.PeriodInfo>();

                // Procesar cada período fallido
                foreach (var period in command.FailedPeriods)
                {
                    if (!recoverySession.ShouldRetryPeriod(period))
                    {
                        stillFailedPeriods.Add(period);
                        continue;
                    }

                    try
                    {
                        // Intentar descargar archivos para el período
                        await _progressService.NotifyMessageAsync($"Reintentando descarga para período {period.DisplayName}", cancellationToken);
                        
                        // Aquí se haría la descarga real usando el WebPortalService
                        // Por ahora simulamos el intento
                        var downloadResult = await RetryDownloadForPeriod(period, command.DownloadPath, cancellationToken);
                        
                        if (downloadResult.Success)
                        {
                            recoverySession.AddRecoveryAttempt(period, true);
                            successfulPeriods.Add(period);
                            await _progressService.NotifyMessageAsync($"✓ Período {period.DisplayName} recuperado exitosamente", cancellationToken);
                        }
                        else
                        {
                            recoverySession.AddRecoveryAttempt(period, false, downloadResult.ErrorMessage);
                            
                            if (!recoverySession.ShouldRetryPeriod(period))
                            {
                                stillFailedPeriods.Add(period);
                                await _progressService.NotifyMessageAsync($"✗ Período {period.DisplayName} falló después de todos los reintentos", cancellationToken);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        recoverySession.AddRecoveryAttempt(period, false, ex.Message);
                        
                        if (!recoverySession.ShouldRetryPeriod(period))
                        {
                            stillFailedPeriods.Add(period);
                        }
                        
                        await _progressService.NotifyMessageAsync($"✗ Error al recuperar período {period.DisplayName}: {ex.Message}", cancellationToken);
                    }
                }

                // Completar o fallar la sesión de recuperación
                if (stillFailedPeriods.Any())
                {
                    recoverySession.FailRecovery($"Algunos períodos no pudieron ser recuperados: {string.Join(", ", stillFailedPeriods.Select(p => p.DisplayName))}");
                }
                else
                {
                    recoverySession.CompleteRecovery();
                }

                await _recoveryService.SaveRecoverySessionAsync(recoverySession, cancellationToken);

                return new StartErrorRecoveryResult(
                    recoverySession.Id,
                    true,
                    ProcessedPeriods: command.FailedPeriods,
                    SuccessfulPeriods: successfulPeriods,
                    StillFailedPeriods: stillFailedPeriods
                );
            }
            catch (Exception ex)
            {
                return new StartErrorRecoveryResult(Guid.Empty, false, ex.Message);
            }
        }

        private async Task<(bool Success, string? ErrorMessage)> RetryDownloadForPeriod(
            Domain.ValueObjects.PeriodInfo period, 
            string downloadPath, 
            CancellationToken cancellationToken)
        {
            try
            {
                // Aquí se implementaría la lógica real de descarga
                // usando el IWebPortalService para descargar los 3 archivos
                // Por ahora simulamos el proceso
                
                await Task.Delay(1000, cancellationToken); // Simular trabajo

                // Verificar que se descargaron los 3 archivos esperados
                var periodFolder = Path.Combine(downloadPath, period.Year.ToString(), period.DisplayName);
                
                if (Directory.Exists(periodFolder))
                {
                    var files = Directory.GetFiles(periodFolder, "*.*", SearchOption.AllDirectories);
                    if (files.Length >= 3) // PDF recibo, PDF CFDI, XML CFDI
                    {
                        return (true, null);
                    }
                }

                return (false, "No se descargaron todos los archivos esperados");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}
