using NominaDownloaderPEIGTO.Application.Interfaces;
using NominaDownloaderPEIGTO.Domain.Entities;
using NominaDownloaderPEIGTO.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace NominaDownloaderPEIGTO.Infrastructure.Services
{
    /// <summary>
    /// Implementación del servicio de recuperación de errores
    /// </summary>
    public class ErrorRecoveryService : IErrorRecoveryService
    {
        private readonly ILogger<ErrorRecoveryService> _logger;
        private readonly string _recoveryPath;

        public ErrorRecoveryService(ILogger<ErrorRecoveryService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _recoveryPath = Path.Combine(Environment.CurrentDirectory, "recovery");
            
            // Crear directorio de recuperación si no existe
            if (!Directory.Exists(_recoveryPath))
            {
                Directory.CreateDirectory(_recoveryPath);
            }
        }

        public Task<ErrorRecoverySession> CreateRecoverySessionAsync(
            Guid originalSessionId, 
            List<PeriodInfo> failedPeriods, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Creando sesión de recuperación para sesión original {OriginalSessionId} con {FailedCount} períodos fallidos", 
                    originalSessionId, failedPeriods.Count);

                var recoverySession = new ErrorRecoverySession(originalSessionId);

                // Agregar los períodos fallidos
                foreach (var period in failedPeriods)
                {
                    var sanitizedPeriodName = SanitizeFolderName($"Periodo_{period.Period:D2}_{period.DisplayName}");
                    var folderPath = Path.Combine(Environment.CurrentDirectory, "downloads", period.Year.ToString(), sanitizedPeriodName);
                    recoverySession.AddFailedAttempt(period, "Carpeta vacía después de la descarga", folderPath);
                }

                _logger.LogInformation("Sesión de recuperación {RecoverySessionId} creada exitosamente", recoverySession.Id);

                return Task.FromResult(recoverySession);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear sesión de recuperación para sesión {OriginalSessionId}", originalSessionId);
                throw;
            }
        }

        public async Task<ErrorRecoverySession> ExecuteRecoveryAsync(Guid recoverySessionId, CancellationToken cancellationToken = default)
        {
            try
            {
                var session = await GetRecoverySessionAsync(recoverySessionId, cancellationToken);
                
                if (session == null)
                {
                    throw new InvalidOperationException($"Sesión de recuperación {recoverySessionId} no encontrada");
                }

                _logger.LogInformation("Ejecutando recuperación para sesión {RecoverySessionId}", recoverySessionId);

                session.StartRecovery();

                var periodsToRetry = session.GetPeriodsToRetry();
                
                foreach (var period in periodsToRetry)
                {
                    try
                    {
                        _logger.LogInformation("Intentando recuperar período {Period}", period.DisplayName);

                        // Aquí se implementaría la lógica real de reintento
                        // Por ahora simulamos el éxito/fallo
                        await Task.Delay(500, cancellationToken);

                        // Simular resultado (70% de éxito)
                        var success = Random.Shared.Next(1, 11) <= 7;
                        
                        if (success)
                        {
                            session.AddRecoveryAttempt(period, true);
                            _logger.LogInformation("Período {Period} recuperado exitosamente", period.DisplayName);
                        }
                        else
                        {
                            session.AddRecoveryAttempt(period, false, "Error simulado de recuperación");
                            _logger.LogWarning("Falló la recuperación del período {Period}", period.DisplayName);
                        }
                    }
                    catch (Exception ex)
                    {
                        session.AddRecoveryAttempt(period, false, ex.Message);
                        _logger.LogError(ex, "Error durante recuperación del período {Period}", period.DisplayName);
                    }
                }

                // Verificar si todos los períodos fueron recuperados
                var stillFailed = session.GetPeriodsToRetry();
                
                if (stillFailed.Any())
                {
                    session.FailRecovery($"No se pudieron recuperar {stillFailed.Count} períodos");
                    _logger.LogWarning("Recuperación fallida para {FailedCount} períodos", stillFailed.Count);
                }
                else
                {
                    session.CompleteRecovery();
                    _logger.LogInformation("Recuperación completada exitosamente para sesión {RecoverySessionId}", recoverySessionId);
                }

                await SaveRecoverySessionAsync(session, cancellationToken);

                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al ejecutar recuperación para sesión {RecoverySessionId}", recoverySessionId);
                throw;
            }
        }

        public Task CleanupFailedFoldersAsync(List<PeriodInfo> periods, string downloadPath, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Limpiando {PeriodCount} carpetas fallidas", periods.Count);

                foreach (var period in periods)
                {
                    var sanitizedPeriodName = SanitizeFolderName($"Periodo_{period.Period:D2}_{period.DisplayName}");
                    var folderPath = Path.Combine(downloadPath, period.Year.ToString(), sanitizedPeriodName);
                    
                    if (Directory.Exists(folderPath))
                    {
                        try
                        {
                            Directory.Delete(folderPath, true);
                            _logger.LogInformation("Carpeta {FolderPath} eliminada exitosamente", folderPath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "No se pudo eliminar la carpeta {FolderPath}", folderPath);
                        }
                    }
                }

                _logger.LogInformation("Limpieza de carpetas completada");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la limpieza de carpetas fallidas");
                throw;
            }
        }

        public async Task<ErrorRecoverySession?> GetRecoverySessionAsync(Guid recoverySessionId, CancellationToken cancellationToken = default)
        {
            try
            {
                var filePath = Path.Combine(_recoveryPath, $"recovery_{recoverySessionId}.json");
                
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("No se encontró sesión de recuperación {RecoverySessionId}", recoverySessionId);
                    return null;
                }

                var json = await File.ReadAllTextAsync(filePath, cancellationToken);
                // TODO: Implementar deserialización completa
                
                _logger.LogInformation("Sesión de recuperación {RecoverySessionId} encontrada", recoverySessionId);
                return null; // Por ahora retornamos null hasta implementar deserialización
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener sesión de recuperación {RecoverySessionId}", recoverySessionId);
                return null;
            }
        }

        public async Task SaveRecoverySessionAsync(ErrorRecoverySession session, CancellationToken cancellationToken = default)
        {
            try
            {
                var filePath = Path.Combine(_recoveryPath, $"recovery_{session.Id}.json");
                var sessionData = new
                {
                    session.Id,
                    session.OriginalSessionId,
                    session.CreatedAt,
                    session.Status,
                    session.MaxRetryAttempts,
                    FailedAttempts = session.FailedAttempts.Select(fa => new
                    {
                        Period = new { fa.Period.Year, fa.Period.Period, fa.Period.DisplayName },
                        fa.ErrorMessage,
                        fa.FolderPath,
                        fa.FailedAt
                    }),
                    RecoveryAttempts = session.RecoveryAttempts.Select(ra => new
                    {
                        Period = new { ra.Period.Year, ra.Period.Period, ra.Period.DisplayName },
                        ra.Success,
                        ra.ErrorMessage,
                        ra.AttemptedAt
                    })
                };

                var json = JsonSerializer.Serialize(sessionData, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(filePath, json, cancellationToken);

                _logger.LogInformation("Sesión de recuperación {SessionId} guardada en {FilePath}", session.Id, filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar sesión de recuperación {SessionId}", session.Id);
                throw;
            }
        }

        /// <summary>
        /// Sanitiza el nombre de una carpeta reemplazando caracteres inválidos
        /// </summary>
        /// <param name="folderName">Nombre original de la carpeta</param>
        /// <returns>Nombre sanitizado válido para el sistema de archivos</returns>
        private string SanitizeFolderName(string folderName)
        {
            // Reemplazar caracteres no válidos con underscore
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var invalidChar in invalidChars)
            {
                folderName = folderName.Replace(invalidChar, '_');
            }
            
            // Reemplazar espacios con underscore
            folderName = folderName.Replace(' ', '_');
            
            // Reemplazar caracteres especiales comunes
            folderName = folderName.Replace('-', '_')
                                   .Replace('(', '_')
                                   .Replace(')', '_')
                                   .Replace(',', '_')
                                   .Replace('.', '_');
            
            // Remover underscores múltiples
            while (folderName.Contains("__"))
            {
                folderName = folderName.Replace("__", "_");
            }
            
            // Remover underscores al inicio y final
            return folderName.Trim('_');
        }
    }
}
