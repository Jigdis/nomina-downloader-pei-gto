using NominaDownloaderPEIGTO.Domain.ValueObjects;

namespace NominaDownloaderPEIGTO.Application.Interfaces
{
    /// <summary>
    /// Servicio para validar archivos descargados
    /// </summary>
    public interface IFileValidationService
    {
        Task<FileMetadata> ValidateFileAsync(string filePath, CancellationToken cancellationToken = default);
        Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default);
        Task<string> CalculateFileHashAsync(string filePath, CancellationToken cancellationToken = default);
        Task<long> GetFileSizeAsync(string filePath, CancellationToken cancellationToken = default);
        Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default);
    }
}
