namespace NominaDownloaderPEIGTO.Domain.Exceptions
{
    /// <summary>
    /// Excepción base del dominio
    /// </summary>
    public abstract class DomainException : Exception
    {
        protected DomainException(string message) : base(message)
        {
        }

        protected DomainException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Excepción cuando fallan las credenciales de login
    /// </summary>
    public class LoginException : DomainException
    {
        public LoginException(string message) : base(message)
        {
        }

        public LoginException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Excepción cuando falla la navegación en el portal
    /// </summary>
    public class NavigationException : DomainException
    {
        public NavigationException(string message) : base(message)
        {
        }

        public NavigationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Excepción cuando falla la descarga de archivos
    /// </summary>
    public class DownloadException : DomainException
    {
        public DownloadException(string message) : base(message)
        {
        }

        public DownloadException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Excepción cuando falla la validación de archivos
    /// </summary>
    public class ValidationException : DomainException
    {
        public ValidationException(string message) : base(message)
        {
        }

        public ValidationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
