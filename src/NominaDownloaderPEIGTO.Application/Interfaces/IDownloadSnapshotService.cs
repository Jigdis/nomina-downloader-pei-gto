using NominaDownloaderPEIGTO.Domain.Entities;
using NominaDownloaderPEIGTO.Domain.ValueObjects;

namespace NominaDownloaderPEIGTO.Application.Interfaces
{
    /// <summary>
    /// Interfaz para el servicio de snapshots de descarga
    /// </summary>
    public interface IDownloadSnapshotService
    {
        /// <summary>
        /// Crea un snapshot inicial antes de comenzar las descargas
        /// </summary>
        Task<DownloadSnapshot> CreateInitialSnapshotAsync(Guid sessionId, List<PeriodInfo> periods, string downloadPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Guarda un snapshot
        /// </summary>
        Task SaveSnapshotAsync(DownloadSnapshot snapshot, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene un snapshot por ID de sesión
        /// </summary>
        Task<DownloadSnapshot?> GetSnapshotBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Analiza las carpetas vacías después de la descarga
        /// </summary>
        Task<List<PeriodInfo>> AnalyzeEmptyFoldersAsync(Guid sessionId, CancellationToken cancellationToken = default);
    }
}
