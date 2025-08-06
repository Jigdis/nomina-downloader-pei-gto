using NominaDownloaderPEIGTO.Domain.ValueObjects;

namespace NominaDownloaderPEIGTO.Domain.Entities
{
    /// <summary>
    /// Entidad que representa un snapshot del estado inicial de descarga
    /// </summary>
    public class DownloadSnapshot
    {
        public Guid Id { get; private set; }
        public Guid SessionId { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public List<PeriodInfo> RequestedPeriods { get; private set; }
        public string DownloadPath { get; private set; }
        public Dictionary<string, FolderSnapshot> InitialFolderState { get; private set; }

        public DownloadSnapshot(Guid sessionId, List<PeriodInfo> requestedPeriods, string downloadPath)
        {
            Id = Guid.NewGuid();
            SessionId = sessionId;
            CreatedAt = DateTime.UtcNow;
            RequestedPeriods = requestedPeriods?.ToList() ?? throw new ArgumentNullException(nameof(requestedPeriods));
            DownloadPath = downloadPath ?? throw new ArgumentNullException(nameof(downloadPath));
            InitialFolderState = new Dictionary<string, FolderSnapshot>();
        }

        public void CaptureInitialState()
        {
            InitialFolderState.Clear();
            
            foreach (var period in RequestedPeriods)
            {
                var folderPath = Path.Combine(DownloadPath, period.Year.ToString(), period.DisplayName);
                var snapshot = new FolderSnapshot(folderPath);
                InitialFolderState[period.GetPeriodKey()] = snapshot;
            }
        }

        public List<string> GetEmptyFolders()
        {
            var emptyFolders = new List<string>();

            foreach (var kvp in InitialFolderState)
            {
                var snapshot = kvp.Value;
                if (Directory.Exists(snapshot.FolderPath))
                {
                    var currentFiles = Directory.GetFiles(snapshot.FolderPath, "*", SearchOption.AllDirectories);
                    
                    // Si la carpeta está vacía o solo tiene archivos que ya estaban antes
                    if (currentFiles.Length == 0 || 
                        currentFiles.All(file => snapshot.ExistingFiles.Contains(Path.GetFileName(file))))
                    {
                        emptyFolders.Add(snapshot.FolderPath);
                    }
                }
            }

            return emptyFolders;
        }

        public List<PeriodInfo> GetPeriodsForEmptyFolders()
        {
            var emptyFolders = GetEmptyFolders();
            var periods = new List<PeriodInfo>();

            foreach (var period in RequestedPeriods)
            {
                var folderPath = Path.Combine(DownloadPath, period.Year.ToString(), period.DisplayName);
                if (emptyFolders.Contains(folderPath))
                {
                    periods.Add(period);
                }
            }

            return periods;
        }
    }

    /// <summary>
    /// Representa el estado de una carpeta en un momento específico
    /// </summary>
    public class FolderSnapshot
    {
        public string FolderPath { get; private set; }
        public List<string> ExistingFiles { get; private set; }
        public DateTime CapturedAt { get; private set; }

        public FolderSnapshot(string folderPath)
        {
            FolderPath = folderPath ?? throw new ArgumentNullException(nameof(folderPath));
            ExistingFiles = new List<string>();
            CapturedAt = DateTime.UtcNow;
            
            CaptureCurrentState();
        }

        private void CaptureCurrentState()
        {
            if (Directory.Exists(FolderPath))
            {
                var files = Directory.GetFiles(FolderPath, "*", SearchOption.AllDirectories);
                ExistingFiles = files.Select(f => Path.GetFileName(f) ?? "").Where(name => !string.IsNullOrEmpty(name)).ToList();
            }
        }
    }
}
