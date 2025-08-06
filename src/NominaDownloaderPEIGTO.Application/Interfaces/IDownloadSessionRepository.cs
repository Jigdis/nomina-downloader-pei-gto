using NominaDownloaderPEIGTO.Domain.Entities;

namespace NominaDownloaderPEIGTO.Application.Interfaces
{
    /// <summary>
    /// Repositorio para gestionar sesiones de descarga
    /// </summary>
    public interface IDownloadSessionRepository
    {
        Task<DownloadSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<DownloadSession> CreateAsync(DownloadSession session, CancellationToken cancellationToken = default);
        Task UpdateAsync(DownloadSession session, CancellationToken cancellationToken = default);
        Task<IEnumerable<DownloadSession>> GetActiveSessionsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<DownloadSession>> GetRecentSessionsAsync(int count = 10, CancellationToken cancellationToken = default);
    }
}
