using NominaDownloaderPEIGTO.Domain.ValueObjects;

namespace NominaDownloaderPEIGTO.Domain.Entities
{
    /// <summary>
    /// Entidad que representa una tarea fallida
    /// </summary>
    public class FailedTask
    {
        public Guid Id { get; private set; }
        public PeriodInfo Period { get; private set; }
        public string ErrorMessage { get; private set; }
        public string? StackTrace { get; private set; }
        public DateTime FailedAt { get; private set; }
        public int AttemptNumber { get; private set; }
        public string? BrowserInfo { get; private set; }

        public FailedTask(
            PeriodInfo period, 
            string errorMessage, 
            int attemptNumber, 
            string? stackTrace = null,
            string? browserInfo = null)
        {
            Id = Guid.NewGuid();
            Period = period ?? throw new ArgumentNullException(nameof(period));
            ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
            AttemptNumber = attemptNumber;
            StackTrace = stackTrace;
            BrowserInfo = browserInfo;
            FailedAt = DateTime.UtcNow;
        }

        public string DisplayMessage => $"Fallo en {Period.DisplayName} (Intento {AttemptNumber}): {ErrorMessage}";
        
        public override string ToString() => DisplayMessage;
    }
}
