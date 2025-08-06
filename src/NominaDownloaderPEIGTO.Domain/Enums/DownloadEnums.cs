namespace NominaDownloaderPEIGTO.Domain.Enums
{
    /// <summary>
    /// Estado del proceso de descarga de un período
    /// </summary>
    public enum DownloadStatus
    {
        /// <summary>
        /// Período pendiente de procesar
        /// </summary>
        Pending,
        
        /// <summary>
        /// Descarga en progreso
        /// </summary>
        InProgress,
        
        /// <summary>
        /// Descarga completada exitosamente
        /// </summary>
        Completed,
        
        /// <summary>
        /// Descarga falló después de todos los reintentos
        /// </summary>
        Failed,
        
        /// <summary>
        /// Período omitido (ya tenía archivos válidos)
        /// </summary>
        Skipped
    }

    /// <summary>
    /// Tipo de archivo descargado
    /// </summary>
    public enum FileType
    {
        /// <summary>
        /// Archivo PDF del recibo de nómina
        /// </summary>
        ReciboPdf,
        
        /// <summary>
        /// Archivo PDF del CFDI
        /// </summary>
        CfdiPdf,
        
        /// <summary>
        /// Archivo XML del CFDI
        /// </summary>
        CfdiXml
    }

    /// <summary>
    /// Resultado de validación de archivos
    /// </summary>
    public enum ValidationResult
    {
        /// <summary>
        /// Validación pendiente
        /// </summary>
        Pending,
        
        /// <summary>
        /// Archivo válido
        /// </summary>
        Valid,
        
        /// <summary>
        /// Archivo inválido por contenido
        /// </summary>
        Invalid,
        
        /// <summary>
        /// Archivo corrupto
        /// </summary>
        Corrupted,
        
        /// <summary>
        /// Archivo no encontrado
        /// </summary>
        Missing,
        
    /// <summary>
    /// Error de validación
    /// </summary>
    Error
}

/// <summary>
/// Estado de la sesión de recuperación de errores
/// </summary>
public enum ErrorRecoveryStatus
{
    /// <summary>
    /// Sesión de recuperación pendiente
    /// </summary>
    Pending,
    
    /// <summary>
    /// Recuperación en progreso
    /// </summary>
    InProgress,
    
    /// <summary>
    /// Recuperación completada exitosamente
    /// </summary>
    Completed,
    
    /// <summary>
    /// Recuperación falló
    /// </summary>
    Failed
}
}
