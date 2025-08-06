using NominaDownloaderPEIGTO.Domain.Enums;
using NominaDownloaderPEIGTO.Domain.ValueObjects;

namespace NominaDownloaderPEIGTO.Domain.Entities
{
    /// <summary>
    /// Entidad que representa una sesión de descarga
    /// </summary>
    public class DownloadSession
    {
        public Guid Id { get; private set; }
        public LoginCredentials Credentials { get; private set; }
        public DownloadConfig Config { get; private set; }
        public DateTime StartedAt { get; private set; }
        public DateTime? CompletedAt { get; private set; }
        public DownloadStatus Status { get; private set; }
        public List<PeriodTask> PeriodTasks { get; private set; }
        public List<DownloadedFile> DownloadedFiles { get; private set; }
        public List<FailedTask> FailedTasks { get; private set; }
        public string? ErrorMessage { get; private set; }

        public DownloadSession(LoginCredentials credentials, DownloadConfig config)
        {
            Id = Guid.NewGuid();
            Credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
            Config = config ?? throw new ArgumentNullException(nameof(config));
            StartedAt = DateTime.UtcNow;
            Status = DownloadStatus.Pending;
            PeriodTasks = new List<PeriodTask>();
            DownloadedFiles = new List<DownloadedFile>();
            FailedTasks = new List<FailedTask>();
        }

        public void AddPeriodTask(PeriodInfo period)
        {
            if (period == null)
                throw new ArgumentNullException(nameof(period));

            if (PeriodTasks.Any(p => p.Period.Year == period.Year && p.Period.Month == period.Month))
                return; // Ya existe

            var task = new PeriodTask(period);
            PeriodTasks.Add(task);
        }

        public void Start()
        {
            if (Status != DownloadStatus.Pending)
                throw new InvalidOperationException("La sesión ya fue iniciada");

            Status = DownloadStatus.InProgress;
        }

        public void Complete()
        {
            if (Status != DownloadStatus.InProgress)
                throw new InvalidOperationException("La sesión no está en progreso");

            Status = DownloadStatus.Completed;
            CompletedAt = DateTime.UtcNow;
        }

        public void Fail(string errorMessage)
        {
            Status = DownloadStatus.Failed;
            ErrorMessage = errorMessage;
            CompletedAt = DateTime.UtcNow;
        }

        public void AddDownloadedFile(DownloadedFile file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            DownloadedFiles.Add(file);
        }

        public void AddFailedTask(FailedTask failedTask)
        {
            if (failedTask == null)
                throw new ArgumentNullException(nameof(failedTask));

            FailedTasks.Add(failedTask);
        }

        // Propiedades calculadas
        public int TotalTasks => PeriodTasks.Count;
        public int CompletedTasks => PeriodTasks.Count(p => p.Status == DownloadStatus.Completed);
        public int FailedTasksCount => FailedTasks.Count;
        public int SuccessfulDownloads => DownloadedFiles.Count;
        public double ProgressPercentage => TotalTasks == 0 ? 0 : (double)CompletedTasks / TotalTasks * 100;
        public TimeSpan? Duration => CompletedAt?.Subtract(StartedAt);
    }
}
