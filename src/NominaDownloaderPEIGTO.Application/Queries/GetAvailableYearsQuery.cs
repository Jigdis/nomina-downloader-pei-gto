using NominaDownloaderPEIGTO.Domain.ValueObjects;

namespace NominaDownloaderPEIGTO.Application.Queries
{
    /// <summary>
    /// Query para obtener los años disponibles en el portal
    /// </summary>
    public record GetAvailableYearsQuery(LoginCredentials Credentials);

    /// <summary>
    /// Resultado de la query de años disponibles
    /// </summary>
    public record GetAvailableYearsResult(
        bool Success,
        IEnumerable<int> Years,
        string? ErrorMessage = null
    );
}
