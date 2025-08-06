using NominaDownloaderPEIGTO.Application.Queries;
using NominaDownloaderPEIGTO.Application.Interfaces;

namespace NominaDownloaderPEIGTO.Application.Handlers
{
    /// <summary>
    /// Handler para la query de analizar carpetas vacías
    /// </summary>
    public class AnalyzeEmptyFoldersHandler
    {
        private readonly IDownloadSnapshotService _snapshotService;

        public AnalyzeEmptyFoldersHandler(IDownloadSnapshotService snapshotService)
        {
            _snapshotService = snapshotService ?? throw new ArgumentNullException(nameof(snapshotService));
        }

        public async Task<AnalyzeEmptyFoldersResult> Handle(
            AnalyzeEmptyFoldersQuery query, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var failedPeriods = await _snapshotService.AnalyzeEmptyFoldersAsync(query.SessionId, cancellationToken);
                var snapshot = await _snapshotService.GetSnapshotBySessionIdAsync(query.SessionId, cancellationToken);
                
                var emptyFolders = snapshot?.GetEmptyFolders() ?? new List<string>();

                return new AnalyzeEmptyFoldersResult(
                    query.SessionId,
                    emptyFolders,
                    failedPeriods,
                    failedPeriods.Any()
                );
            }
            catch (Exception)
            {
                // En caso de error, retornamos un resultado vacío pero válido
                return new AnalyzeEmptyFoldersResult(
                    query.SessionId,
                    new List<string>(),
                    new List<Domain.ValueObjects.PeriodInfo>(),
                    false
                );
            }
        }
    }
}
