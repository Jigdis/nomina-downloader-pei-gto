using NominaDownloaderPEIGTO.Domain.ValueObjects;

namespace NominaDownloaderPEIGTO.Application.Commands
{
    /// <summary>
    /// Comando para iniciar una sesión de descarga
    /// </summary>
    public record StartDownloadSessionCommand
    {
        public LoginCredentials Credentials { get; init; }
        public DownloadConfig Config { get; init; }
        public IEnumerable<PeriodInfo> Periods { get; init; } = new List<PeriodInfo>();

        public StartDownloadSessionCommand(
            LoginCredentials credentials, 
            DownloadConfig config, 
            IEnumerable<PeriodInfo> periods)
        {
            Credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Periods = periods ?? throw new ArgumentNullException(nameof(periods));
        }
    }

    /// <summary>
    /// Resultado del comando de iniciar descarga
    /// </summary>
    public record StartDownloadSessionResult
    {
        public Guid SessionId { get; init; }
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }

        public StartDownloadSessionResult(Guid sessionId, bool success, string? errorMessage = null)
        {
            SessionId = sessionId;
            Success = success;
            ErrorMessage = errorMessage;
        }
    }
}
