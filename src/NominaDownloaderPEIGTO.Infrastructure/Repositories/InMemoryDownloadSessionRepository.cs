using System.Collections.Concurrent;
using NominaDownloaderPEIGTO.Application.Interfaces;
using NominaDownloaderPEIGTO.Domain.Entities;
using NominaDownloaderPEIGTO.Domain.Enums;

namespace NominaDownloaderPEIGTO.Infrastructure.Repositories
{
    /// <summary>
    /// Implementación en memoria del repositorio de sesiones de descarga
    /// </summary>
    public class InMemoryDownloadSessionRepository : IDownloadSessionRepository
    {
        private readonly ConcurrentDictionary<Guid, DownloadSession> _sessions = new();

        public Task<DownloadSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _sessions.TryGetValue(id, out var session);
            return Task.FromResult(session);
        }

        public Task<DownloadSession> CreateAsync(DownloadSession session, CancellationToken cancellationToken = default)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            _sessions.TryAdd(session.Id, session);
            return Task.FromResult(session);
        }

        public Task UpdateAsync(DownloadSession session, CancellationToken cancellationToken = default)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            _sessions.AddOrUpdate(session.Id, session, (key, oldValue) => session);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<DownloadSession>> GetActiveSessionsAsync(CancellationToken cancellationToken = default)
        {
            var activeSessions = _sessions.Values
                .Where(s => s.Status == DownloadStatus.InProgress || s.Status == DownloadStatus.Pending)
                .ToList();

            return Task.FromResult<IEnumerable<DownloadSession>>(activeSessions);
        }

        public Task<IEnumerable<DownloadSession>> GetRecentSessionsAsync(int count = 10, CancellationToken cancellationToken = default)
        {
            var recentSessions = _sessions.Values
                .OrderByDescending(s => s.StartedAt)
                .Take(count)
                .ToList();

            return Task.FromResult<IEnumerable<DownloadSession>>(recentSessions);
        }
    }
}
