using NominaDownloaderPEIGTO.Application.Commands;
using NominaDownloaderPEIGTO.Application.Interfaces;

namespace NominaDownloaderPEIGTO.Application.Handlers
{
    /// <summary>
    /// Handler para el comando de crear snapshot
    /// </summary>
    public class CreateSnapshotHandler
    {
        private readonly IDownloadSnapshotService _snapshotService;

        public CreateSnapshotHandler(IDownloadSnapshotService snapshotService)
        {
            _snapshotService = snapshotService ?? throw new ArgumentNullException(nameof(snapshotService));
        }

        public async Task<CreateSnapshotResult> Handle(
            CreateSnapshotCommand command, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var snapshot = await _snapshotService.CreateInitialSnapshotAsync(
                    command.SessionId,
                    command.Periods,
                    command.DownloadPath,
                    cancellationToken);

                await _snapshotService.SaveSnapshotAsync(snapshot, cancellationToken);

                return new CreateSnapshotResult(snapshot.Id, true);
            }
            catch (Exception ex)
            {
                return new CreateSnapshotResult(Guid.Empty, false, ex.Message);
            }
        }
    }
}
