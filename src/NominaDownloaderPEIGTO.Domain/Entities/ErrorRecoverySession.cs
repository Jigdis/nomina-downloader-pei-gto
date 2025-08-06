using NominaDownloaderPEIGTO.Domain.ValueObjects;
using NominaDownloaderPEIGTO.Domain.Enums;

namespace NominaDownloaderPEIGTO.Domain.Entities
{
    /// <summary>
    /// Entidad que maneja la recuperación de errores en descargas fallidas
    /// </summary>
    public class ErrorRecoverySession
    {
        public Guid Id { get; private set; }
        public Guid OriginalSessionId { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public List<FailedDownloadAttempt> FailedAttempts { get; private set; }
        public List<RecoveryAttempt> RecoveryAttempts { get; private set; }
        public ErrorRecoveryStatus Status { get; private set; }
        public int MaxRetryAttempts { get; private set; }

        public ErrorRecoverySession(Guid originalSessionId, int maxRetryAttempts = 3)
        {
            Id = Guid.NewGuid();
            OriginalSessionId = originalSessionId;
            CreatedAt = DateTime.UtcNow;
            FailedAttempts = new List<FailedDownloadAttempt>();
            RecoveryAttempts = new List<RecoveryAttempt>();
            Status = ErrorRecoveryStatus.Pending;
            MaxRetryAttempts = maxRetryAttempts;
        }

        public void AddFailedAttempt(PeriodInfo period, string errorMessage, string folderPath)
        {
            var attempt = new FailedDownloadAttempt(period, errorMessage, folderPath);
            FailedAttempts.Add(attempt);
        }

        public void StartRecovery()
        {
            if (Status != ErrorRecoveryStatus.Pending)
                throw new InvalidOperationException("La sesión de recuperación ya fue iniciada");

            Status = ErrorRecoveryStatus.InProgress;
        }

        public void CompleteRecovery()
        {
            Status = ErrorRecoveryStatus.Completed;
        }

        public void FailRecovery(string errorMessage)
        {
            Status = ErrorRecoveryStatus.Failed;
        }

        public List<PeriodInfo> GetPeriodsToRetry()
        {
            return FailedAttempts
                .Where(fa => RecoveryAttempts.Count(ra => ra.Period.GetPeriodKey() == fa.Period.GetPeriodKey()) < MaxRetryAttempts)
                .Select(fa => fa.Period)
                .ToList();
        }

        public void AddRecoveryAttempt(PeriodInfo period, bool success, string? errorMessage = null)
        {
            var attempt = new RecoveryAttempt(period, success, errorMessage);
            RecoveryAttempts.Add(attempt);
        }

        public bool ShouldRetryPeriod(PeriodInfo period)
        {
            var attemptCount = RecoveryAttempts.Count(ra => ra.Period.GetPeriodKey() == period.GetPeriodKey());
            return attemptCount < MaxRetryAttempts;
        }

        public void CleanupFailedFolder(string folderPath)
        {
            try
            {
                if (Directory.Exists(folderPath))
                {
                    Directory.Delete(folderPath, true);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the recovery process
                throw new InvalidOperationException($"Error al limpiar carpeta {folderPath}: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Representa un intento fallido de descarga
    /// </summary>
    public class FailedDownloadAttempt
    {
        public PeriodInfo Period { get; private set; }
        public string ErrorMessage { get; private set; }
        public string FolderPath { get; private set; }
        public DateTime FailedAt { get; private set; }

        public FailedDownloadAttempt(PeriodInfo period, string errorMessage, string folderPath)
        {
            Period = period ?? throw new ArgumentNullException(nameof(period));
            ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
            FolderPath = folderPath ?? throw new ArgumentNullException(nameof(folderPath));
            FailedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Representa un intento de recuperación
    /// </summary>
    public class RecoveryAttempt
    {
        public PeriodInfo Period { get; private set; }
        public bool Success { get; private set; }
        public string? ErrorMessage { get; private set; }
        public DateTime AttemptedAt { get; private set; }

        public RecoveryAttempt(PeriodInfo period, bool success, string? errorMessage = null)
        {
            Period = period ?? throw new ArgumentNullException(nameof(period));
            Success = success;
            ErrorMessage = errorMessage;
            AttemptedAt = DateTime.UtcNow;
        }
    }
}
