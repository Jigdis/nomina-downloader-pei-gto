using NominaDownloaderPEIGTO.Domain.Enums;
using NominaDownloaderPEIGTO.Domain.ValueObjects;

namespace NominaDownloaderPEIGTO.Domain.Entities
{
    /// <summary>
    /// Entidad que representa una tarea de descarga por período
    /// </summary>
    public class PeriodTask
    {
        public Guid Id { get; private set; }
        public PeriodInfo Period { get; private set; }
        public DownloadStatus Status { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? StartedAt { get; private set; }
        public DateTime? CompletedAt { get; private set; }
        public int AttemptCount { get; private set; }
        public string? ErrorMessage { get; private set; }
        public List<DownloadedFile> Files { get; private set; }

        public PeriodTask(PeriodInfo period)
        {
            Id = Guid.NewGuid();
            Period = period ?? throw new ArgumentNullException(nameof(period));
            Status = DownloadStatus.Pending;
            CreatedAt = DateTime.UtcNow;
            AttemptCount = 0;
            Files = new List<DownloadedFile>();
        }

        public void Start()
        {
            if (Status == DownloadStatus.InProgress)
                return; // Ya está en progreso

            Status = DownloadStatus.InProgress;
            StartedAt = DateTime.UtcNow;
            AttemptCount++;
        }

        public void Complete()
        {
            if (Status != DownloadStatus.InProgress)
                throw new InvalidOperationException("La tarea no está en progreso");

            Status = DownloadStatus.Completed;
            CompletedAt = DateTime.UtcNow;
        }

        public void Fail(string errorMessage)
        {
            Status = DownloadStatus.Failed;
            ErrorMessage = errorMessage;
            CompletedAt = DateTime.UtcNow;
        }

        public void Reset()
        {
            Status = DownloadStatus.Pending;
            StartedAt = null;
            CompletedAt = null;
            ErrorMessage = null;
            Files.Clear();
        }

        public void AddFile(DownloadedFile file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            Files.Add(file);
        }

        public bool CanRetry(int maxRetries) => AttemptCount < maxRetries;
        public TimeSpan? Duration => CompletedAt?.Subtract(StartedAt ?? CreatedAt);
        public bool HasFiles => Files.Any();
    }
}
