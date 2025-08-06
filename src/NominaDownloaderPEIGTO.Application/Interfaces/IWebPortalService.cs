using NominaDownloaderPEIGTO.Domain.ValueObjects;

namespace NominaDownloaderPEIGTO.Application.Interfaces
{
    /// <summary>
    /// Servicio para interactuar con el portal web
    /// </summary>
    public interface IWebPortalService
    {
        Task<bool> LoginAsync(LoginCredentials credentials, CancellationToken cancellationToken = default);
        Task<IEnumerable<int>> GetAvailableYearsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<PeriodInfo>> GetAvailablePeriodsAsync(int year, CancellationToken cancellationToken = default);
        Task<FileMetadata> DownloadFileAsync(PeriodInfo period, string downloadPath, CancellationToken cancellationToken = default);
        Task LogoutAsync(CancellationToken cancellationToken = default);
        Task<bool> ValidateSessionAsync(CancellationToken cancellationToken = default);
    }
}
