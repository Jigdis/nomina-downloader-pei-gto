using NominaDownloaderPEIGTO.Domain.ValueObjects;

namespace NominaDownloaderPEIGTO.Application.Commands
{
    /// <summary>
    /// Comando para iniciar recuperación de errores
    /// </summary>
    public record StartErrorRecoveryCommand(
        Guid OriginalSessionId,
        List<PeriodInfo> FailedPeriods,
        string DownloadPath,
        int MaxRetryAttempts = 3
    );

    /// <summary>
    /// Resultado del comando de recuperación de errores
    /// </summary>
    public record StartErrorRecoveryResult(
        Guid RecoverySessionId,
        bool Success,
        string? ErrorMessage = null,
        List<PeriodInfo>? ProcessedPeriods = null,
        List<PeriodInfo>? SuccessfulPeriods = null,
        List<PeriodInfo>? StillFailedPeriods = null
    );
}
