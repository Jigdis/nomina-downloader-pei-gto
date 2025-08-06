using NominaDownloaderPEIGTO.Application.Interfaces;
using NominaDownloaderPEIGTO.Application.Queries;

namespace NominaDownloaderPEIGTO.Application.Handlers
{
    /// <summary>
    /// Handler para obtener años disponibles
    /// </summary>
    public class GetAvailableYearsHandler
    {
        private readonly IWebPortalService _webPortalService;

        public GetAvailableYearsHandler(IWebPortalService webPortalService)
        {
            _webPortalService = webPortalService ?? throw new ArgumentNullException(nameof(webPortalService));
        }

        public async Task<GetAvailableYearsResult> Handle(GetAvailableYearsQuery query, CancellationToken cancellationToken = default)
        {
            try
            {
                // Iniciar sesión
                var loginSuccess = await _webPortalService.LoginAsync(query.Credentials, cancellationToken);
                if (!loginSuccess)
                {
                    var error = "Error al iniciar sesión en el portal";
                    return new GetAvailableYearsResult(false, Enumerable.Empty<int>(), error);
                }

                // Obtener años disponibles
                var years = await _webPortalService.GetAvailableYearsAsync(cancellationToken);
                
                return new GetAvailableYearsResult(true, years);
            }
            catch (Exception ex)
            {
                var error = $"Error obteniendo años disponibles: {ex.Message}";
                return new GetAvailableYearsResult(false, Enumerable.Empty<int>(), error);
            }
        }
    }
}
