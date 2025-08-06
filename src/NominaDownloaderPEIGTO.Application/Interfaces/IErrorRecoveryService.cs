using NominaDownloaderPEIGTO.Domain.Entities;
using NominaDownloaderPEIGTO.Domain.ValueObjects;

namespace NominaDownloaderPEIGTO.Application.Interfaces
{
    /// <summary>
    /// Interfaz para el servicio de recuperación de errores
    /// </summary>
    public interface IErrorRecoveryService
    {
        /// <summary>
        /// Crea una nueva sesión de recuperación de errores
        /// </summary>
        Task<ErrorRecoverySession> CreateRecoverySessionAsync(Guid originalSessionId, List<PeriodInfo> failedPeriods, CancellationToken cancellationToken = default);

        /// <summary>
        /// Ejecuta el proceso de recuperación para los períodos fallidos
        /// </summary>
        Task<ErrorRecoverySession> ExecuteRecoveryAsync(Guid recoverySessionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Limpia las carpetas de los períodos fallidos
        /// </summary>
        Task CleanupFailedFoldersAsync(List<PeriodInfo> periods, string downloadPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene una sesión de recuperación por ID
        /// </summary>
        Task<ErrorRecoverySession?> GetRecoverySessionAsync(Guid recoverySessionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Guarda una sesión de recuperación
        /// </summary>
        Task SaveRecoverySessionAsync(ErrorRecoverySession session, CancellationToken cancellationToken = default);
    }
}
