using NominaDownloaderPEIGTO.Domain.Entities;

namespace NominaDownloaderPEIGTO.Application.Interfaces
{
    /// <summary>
    /// Servicio para gestionar el progreso de descarga
    /// </summary>
    public interface IProgressService
    {
        Task NotifySessionStartedAsync(DownloadSession session, CancellationToken cancellationToken = default);
        Task NotifyTaskStartedAsync(PeriodTask task, CancellationToken cancellationToken = default);
        Task NotifyTaskCompletedAsync(PeriodTask task, CancellationToken cancellationToken = default);
        Task NotifyTaskFailedAsync(FailedTask failedTask, CancellationToken cancellationToken = default);
        Task NotifyFileDownloadedAsync(DownloadedFile file, CancellationToken cancellationToken = default);
        Task NotifySessionCompletedAsync(DownloadSession session, CancellationToken cancellationToken = default);
        Task NotifyMessageAsync(string message, CancellationToken cancellationToken = default);
    }
}
