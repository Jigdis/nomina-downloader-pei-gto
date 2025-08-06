namespace NominaDownloaderPEIGTO.Application.Queries
{
    /// <summary>
    /// Query para analizar carpetas vacías
    /// </summary>
    public record AnalyzeEmptyFoldersQuery(
        Guid SessionId
    );

    /// <summary>
    /// Resultado del análisis de carpetas vacías
    /// </summary>
    public record AnalyzeEmptyFoldersResult(
        Guid SessionId,
        List<string> EmptyFolders,
        List<Domain.ValueObjects.PeriodInfo> FailedPeriods,
        bool HasEmptyFolders
    );
}
