using NominaDownloaderPEIGTO.Domain.ValueObjects;

namespace NominaDownloaderPEIGTO.Application.Commands
{
    /// <summary>
    /// Comando para crear un snapshot inicial
    /// </summary>
    public record CreateSnapshotCommand(
        Guid SessionId,
        List<PeriodInfo> Periods,
        string DownloadPath
    );

    /// <summary>
    /// Resultado del comando de crear snapshot
    /// </summary>
    public record CreateSnapshotResult(
        Guid SnapshotId,
        bool Success,
        string? ErrorMessage = null
    );
}
