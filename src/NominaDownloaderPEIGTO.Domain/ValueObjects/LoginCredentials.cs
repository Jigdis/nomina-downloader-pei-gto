namespace NominaDownloaderPEIGTO.Domain.ValueObjects
{
    /// <summary>
    /// Objeto de valor para credenciales de login
    /// </summary>
    public record LoginCredentials
    {
        public string Username { get; }
        public string Password { get; }

        public LoginCredentials(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("El nombre de usuario no puede estar vacío", nameof(username));
            
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("La contraseña no puede estar vacía", nameof(password));

            Username = username.Trim();
            Password = password;
        }
    }
}
