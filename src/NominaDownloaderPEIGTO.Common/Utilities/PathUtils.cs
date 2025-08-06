namespace NominaDownloaderPEIGTO.Common.Utilities;

/// <summary>
/// Utilidades para manipular rutas y nombres de archivos/carpetas
/// </summary>
public static class PathUtils
{
    /// <summary>
    /// Sanitiza el nombre de una carpeta reemplazando caracteres inválidos
    /// </summary>
    /// <param name="folderName">Nombre original de la carpeta</param>
    /// <returns>Nombre sanitizado válido para el sistema de archivos</returns>
    public static string SanitizeFolderName(string folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName))
        {
            throw new ArgumentException("El nombre de la carpeta no puede ser nulo o vacío", nameof(folderName));
        }

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
