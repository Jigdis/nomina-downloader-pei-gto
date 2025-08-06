using NominaDownloaderPEIGTO.Domain.Enums;

namespace NominaDownloaderPEIGTO.Domain.ValueObjects
{
    /// <summary>
    /// Objeto de valor para configuración de descarga
    /// </summary>
    public record DownloadConfig
    {
        public string DownloadPath { get; }
        public int MaxParallelBrowsers { get; }
        public int MaxRetryAttempts { get; }
        public TimeSpan TimeoutPerDownload { get; }
        public bool ValidateDownloads { get; }
        public FileType PreferredFileType { get; }

        public DownloadConfig(
            string downloadPath,
            int maxParallelBrowsers = 16,
            int maxRetryAttempts = 3,
            TimeSpan timeoutPerDownload = default,
            bool validateDownloads = true,
            FileType preferredFileType = FileType.ReciboPdf)
        {
            if (string.IsNullOrWhiteSpace(downloadPath))
                throw new ArgumentException("La ruta de descarga no puede estar vacía", nameof(downloadPath));
            
            if (maxParallelBrowsers <= 0)
                throw new ArgumentException("El número de navegadores paralelos debe ser mayor a 0", nameof(maxParallelBrowsers));
            
            if (maxRetryAttempts < 0)
                throw new ArgumentException("El número de reintentos no puede ser negativo", nameof(maxRetryAttempts));

            DownloadPath = downloadPath;
            MaxParallelBrowsers = maxParallelBrowsers;
            MaxRetryAttempts = maxRetryAttempts;
            TimeoutPerDownload = timeoutPerDownload == default ? TimeSpan.FromMinutes(5) : timeoutPerDownload;
            ValidateDownloads = validateDownloads;
            PreferredFileType = preferredFileType;
        }
    }
}
