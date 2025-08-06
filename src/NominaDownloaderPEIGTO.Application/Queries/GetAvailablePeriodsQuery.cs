using NominaDownloaderPEIGTO.Domain.ValueObjects;

namespace NominaDownloaderPEIGTO.Application.Queries
{
    /// <summary>
    /// Consulta para obtener los períodos disponibles de un año específico
    /// </summary>
    public record GetAvailablePeriodsQuery
    {
        public LoginCredentials Credentials { get; init; }
        public int Year { get; init; }

        public GetAvailablePeriodsQuery(LoginCredentials credentials, int year)
        {
            Credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
            Year = year;
        }
    }

    /// <summary>
    /// Resultado de la consulta de períodos disponibles
    /// </summary>
    public record GetAvailablePeriodsResult
    {
        public IEnumerable<PeriodInfo> Periods { get; init; }
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }

        public GetAvailablePeriodsResult(IEnumerable<PeriodInfo> periods, bool success, string? errorMessage = null)
        {
            Periods = periods ?? new List<PeriodInfo>();
            Success = success;
            ErrorMessage = errorMessage;
        }
    }
}
