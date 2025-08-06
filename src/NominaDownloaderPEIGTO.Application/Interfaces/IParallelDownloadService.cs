namespace NominaDownloaderPEIGTO.Application.Interfaces
{
    /// <summary>
    /// Interfaz para el servicio de descarga paralela
    /// </summary>
    public interface IParallelDownloadService
    {
        /// <summary>
        /// Procesa una sesión de descarga de forma asíncrona
        /// </summary>
        /// <param name="sessionId">ID de la sesión a procesar</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Task que representa la operación asíncrona</returns>
        Task ProcessSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
    }
}
