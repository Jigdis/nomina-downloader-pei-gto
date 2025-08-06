using NominaDownloaderPEIGTO.Domain.Enums;

namespace NominaDownloaderPEIGTO.Domain.ValueObjects
{
    /// <summary>
    /// Objeto de valor para metadatos de archivo
    /// </summary>
    public record FileMetadata
    {
        public string FileName { get; }
        public string FilePath { get; }
        public long FileSize { get; }
        public FileType FileType { get; }
        public DateTime DownloadedAt { get; }
        public string Hash { get; }

        public FileMetadata(
            string fileName,
            string filePath,
            long fileSize,
            FileType fileType,
            DateTime downloadedAt,
            string hash)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("El nombre del archivo no puede estar vacío", nameof(fileName));
            
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("La ruta del archivo no puede estar vacía", nameof(filePath));
            
            if (fileSize < 0)
                throw new ArgumentException("El tamaño del archivo no puede ser negativo", nameof(fileSize));
            
            if (string.IsNullOrWhiteSpace(hash))
                throw new ArgumentException("El hash del archivo no puede estar vacío", nameof(hash));

            FileName = fileName;
            FilePath = filePath;
            FileSize = fileSize;
            FileType = fileType;
            DownloadedAt = downloadedAt;
            Hash = hash;
        }

        public bool IsValid => FileSize > 0 && !string.IsNullOrWhiteSpace(Hash);
    }
}
