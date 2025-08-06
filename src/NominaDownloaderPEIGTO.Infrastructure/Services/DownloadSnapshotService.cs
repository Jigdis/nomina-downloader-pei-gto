using NominaDownloaderPEIGTO.Application.Interfaces;
using NominaDownloaderPEIGTO.Domain.Entities;
using NominaDownloaderPEIGTO.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace NominaDownloaderPEIGTO.Infrastructure.Services
{
    /// <summary>
    /// Implementación del servicio de snapshots de descarga
    /// </summary>
    public class DownloadSnapshotService : IDownloadSnapshotService
    {
        private readonly ILogger<DownloadSnapshotService> _logger;
        private readonly string _snapshotsPath;

        public DownloadSnapshotService(ILogger<DownloadSnapshotService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _snapshotsPath = Path.Combine(Environment.CurrentDirectory, "snapshots");
            
            // Crear directorio de snapshots si no existe
            if (!Directory.Exists(_snapshotsPath))
            {
                Directory.CreateDirectory(_snapshotsPath);
            }
        }

        public Task<DownloadSnapshot> CreateInitialSnapshotAsync(
            Guid sessionId, 
            List<PeriodInfo> periods, 
            string downloadPath, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Creando snapshot inicial para sesión {SessionId}", sessionId);

                var snapshot = new DownloadSnapshot(sessionId, periods, downloadPath);
                snapshot.CaptureInitialState();

                _logger.LogInformation("Snapshot inicial creado con {PeriodCount} períodos", periods.Count);

                return Task.FromResult(snapshot);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear snapshot inicial para sesión {SessionId}", sessionId);
                throw;
            }
        }

        public async Task SaveSnapshotAsync(DownloadSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            try
            {
                var filePath = Path.Combine(_snapshotsPath, $"snapshot_{snapshot.SessionId}.json");
                var snapshotData = new
                {
                    snapshot.Id,
                    snapshot.SessionId,
                    snapshot.CreatedAt,
                    snapshot.RequestedPeriods,
                    snapshot.DownloadPath,
                    snapshot.InitialFolderState
                };

                var json = JsonSerializer.Serialize(snapshotData, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(filePath, json, cancellationToken);

                _logger.LogInformation("Snapshot guardado en {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar snapshot {SnapshotId}", snapshot.Id);
                throw;
            }
        }

        public async Task<DownloadSnapshot?> GetSnapshotBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            try
            {
                var filePath = Path.Combine(_snapshotsPath, $"snapshot_{sessionId}.json");
                
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("No se encontró snapshot para sesión {SessionId}", sessionId);
                    return null;
                }

                var json = await File.ReadAllTextAsync(filePath, cancellationToken);
                var snapshotData = JsonSerializer.Deserialize<dynamic>(json);

                // Aquí tendríamos que deserializar correctamente el snapshot
                // Por simplicidad, retornamos null por ahora
                _logger.LogInformation("Snapshot encontrado para sesión {SessionId}", sessionId);
                return null; // TODO: Implementar deserialización completa
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener snapshot para sesión {SessionId}", sessionId);
                return null;
            }
        }

        public async Task<List<PeriodInfo>> AnalyzeEmptyFoldersAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            try
            {
                var snapshot = await GetSnapshotBySessionIdAsync(sessionId, cancellationToken);
                
                if (snapshot == null)
                {
                    _logger.LogWarning("No se puede analizar carpetas vacías - snapshot no encontrado para sesión {SessionId}", sessionId);
                    return new List<PeriodInfo>();
                }

                var emptyFolderPeriods = snapshot.GetPeriodsForEmptyFolders();
                
                _logger.LogInformation("Se encontraron {EmptyFolderCount} carpetas vacías para sesión {SessionId}", 
                    emptyFolderPeriods.Count, sessionId);

                return emptyFolderPeriods;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al analizar carpetas vacías para sesión {SessionId}", sessionId);
                return new List<PeriodInfo>();
            }
        }
    }
}
