using NominaDownloaderPEIGTO.Application.Commands;
using NominaDownloaderPEIGTO.Application.Interfaces;
using NominaDownloaderPEIGTO.Domain.Entities;

namespace NominaDownloaderPEIGTO.Application.Handlers
{
    /// <summary>
    /// Handler para el comando de iniciar sesión de descarga
    /// </summary>
    public class StartDownloadSessionHandler
    {
        private readonly IDownloadSessionRepository _sessionRepository;
        private readonly IWebPortalService _webPortalService;
        private readonly IProgressService _progressService;
        private readonly IParallelDownloadService _downloadService;

        public StartDownloadSessionHandler(
            IDownloadSessionRepository sessionRepository,
            IWebPortalService webPortalService,
            IProgressService progressService,
            IParallelDownloadService downloadService)
        {
            _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
            _webPortalService = webPortalService ?? throw new ArgumentNullException(nameof(webPortalService));
            _progressService = progressService ?? throw new ArgumentNullException(nameof(progressService));
            _downloadService = downloadService ?? throw new ArgumentNullException(nameof(downloadService));
        }

        public async Task<StartDownloadSessionResult> Handle(
            StartDownloadSessionCommand command, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Crear nueva sesión
                var session = new DownloadSession(command.Credentials, command.Config);

                // Agregar períodos a la sesión
                foreach (var period in command.Periods)
                {
                    session.AddPeriodTask(period);
                }

                // Guardar sesión
                await _sessionRepository.CreateAsync(session, cancellationToken);

                // Iniciar sesión
                session.Start();
                await _sessionRepository.UpdateAsync(session, cancellationToken);

                // Notificar inicio
                await _progressService.NotifySessionStartedAsync(session, cancellationToken);

                // Ejecutar la descarga
                await _downloadService.ProcessSessionAsync(session.Id, cancellationToken);

                return new StartDownloadSessionResult(session.Id, true);
            }
            catch (Exception ex)
            {
                return new StartDownloadSessionResult(Guid.Empty, false, ex.Message);
            }
        }
    }
}
