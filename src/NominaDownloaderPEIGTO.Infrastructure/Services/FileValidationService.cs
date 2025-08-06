using NominaDownloaderPEIGTO.Application.Interfaces;
using NominaDownloaderPEIGTO.Domain.Enums;
using NominaDownloaderPEIGTO.Domain.ValueObjects;
using System.Security.Cryptography;

namespace NominaDownloaderPEIGTO.Infrastructure.Services
{
    /// <summary>
    /// Implementación del servicio de validación de archivos
    /// </summary>
    public class FileValidationService : IFileValidationService
    {
        public async Task<FileMetadata> ValidateFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (!await FileExistsAsync(filePath, cancellationToken))
            {
                throw new FileNotFoundException($"Archivo no encontrado: {filePath}");
            }

            var fileInfo = new FileInfo(filePath);
            var fileSize = await GetFileSizeAsync(filePath, cancellationToken);
            var hash = await CalculateFileHashAsync(filePath, cancellationToken);
            var fileType = GetFileType(fileInfo.Extension);

            return new FileMetadata(
                fileName: fileInfo.Name,
                filePath: filePath,
                fileSize: fileSize,
                fileType: fileType,
                downloadedAt: fileInfo.CreationTime,
                hash: hash
            );
        }

        public async Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => File.Exists(filePath), cancellationToken);
        }

        public async Task<string> CalculateFileHashAsync(string filePath, CancellationToken cancellationToken = default)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filePath);
            
            var hashBytes = await Task.Run(() => md5.ComputeHash(stream), cancellationToken);
            return Convert.ToHexString(hashBytes);
        }

        public async Task<long> GetFileSizeAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                var fileInfo = new FileInfo(filePath);
                return fileInfo.Length;
            }, cancellationToken);
        }

        public async Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            await Task.Run(() =>
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }, cancellationToken);
        }

        private FileType GetFileType(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".pdf" => FileType.ReciboPdf,
                ".xml" => FileType.CfdiXml,
                _ => FileType.ReciboPdf
            };
        }
    }
}
