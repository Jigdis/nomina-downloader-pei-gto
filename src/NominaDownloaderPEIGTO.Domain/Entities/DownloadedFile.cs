using NominaDownloaderPEIGTO.Domain.Enums;
using NominaDownloaderPEIGTO.Domain.ValueObjects;

namespace NominaDownloaderPEIGTO.Domain.Entities
{
    /// <summary>
    /// Entidad que representa un archivo descargado
    /// </summary>
    public class DownloadedFile
    {
        public Guid Id { get; private set; }
        public PeriodInfo Period { get; private set; }
        public FileMetadata Metadata { get; private set; }
        public ValidationResult ValidationStatus { get; private set; }
        public DateTime DownloadedAt { get; private set; }
        public string? ValidationMessage { get; private set; }

        public DownloadedFile(PeriodInfo period, FileMetadata metadata)
        {
            Id = Guid.NewGuid();
            Period = period ?? throw new ArgumentNullException(nameof(period));
            Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
            DownloadedAt = DateTime.UtcNow;
            ValidationStatus = ValidationResult.Pending;
        }

        public void MarkAsValid()
        {
            ValidationStatus = ValidationResult.Valid;
            ValidationMessage = null;
        }

        public void MarkAsInvalid(string reason)
        {
            ValidationStatus = ValidationResult.Invalid;
            ValidationMessage = reason;
        }

        public void MarkAsCorrupted(string reason)
        {
            ValidationStatus = ValidationResult.Corrupted;
            ValidationMessage = reason;
        }

        public bool IsValid => ValidationStatus == ValidationResult.Valid;
        public bool RequiresValidation => ValidationStatus == ValidationResult.Pending;
        public string DisplayName => $"{Period.DisplayName} - {Metadata.FileName}";
    }
}
