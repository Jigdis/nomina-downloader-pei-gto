using NominaDownloaderPEIGTO.Domain.ValueObjects;

namespace NominaDownloaderPEIGTO.Console.Services;

public enum DownloadScope
{
    SpecificPeriods,
    EntireYear,
    AllYears
}

public interface IUserInteractionService
{
    LoginCredentials? GetCredentialsFromUser();
    DownloadScope SelectDownloadScope();
    int? SelectYear(IEnumerable<int> availableYears);
    IEnumerable<PeriodInfo> SelectPeriods(IEnumerable<PeriodInfo> availablePeriods);
}
