using NominaDownloaderPEIGTO.Application.Interfaces;
using NominaDownloaderPEIGTO.Application.Queries;
using NominaDownloaderPEIGTO.Domain.Exceptions;

namespace NominaDownloaderPEIGTO.Application.Handlers
{
    /// <summary>
    /// Handler para la consulta de períodos disponibles
    /// </summary>
    public class GetAvailablePeriodsHandler
    {
        private readonly IWebPortalService _webPortalService;

        public GetAvailablePeriodsHandler(IWebPortalService webPortalService)
        {
            _webPortalService = webPortalService ?? throw new ArgumentNullException(nameof(webPortalService));
        }

        public async Task<GetAvailablePeriodsResult> Handle(
            GetAvailablePeriodsQuery query, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Verificar si ya hay una sesión válida, si no hacer login
                var sessionValid = await _webPortalService.ValidateSessionAsync(cancellationToken);
                if (!sessionValid)
                {
                    var loginSuccess = await _webPortalService.LoginAsync(query.Credentials, cancellationToken);
                    if (!loginSuccess)
                    {
                        throw new LoginException("Falló el login al portal");
                    }
                }

                // Obtener períodos del año específico
                var periods = await _webPortalService.GetAvailablePeriodsAsync(query.Year, cancellationToken);

                return new GetAvailablePeriodsResult(periods, true);
            }
            catch (Exception ex)
            {
                return new GetAvailablePeriodsResult(new List<Domain.ValueObjects.PeriodInfo>(), false, ex.Message);
            }
        }
    }
}
