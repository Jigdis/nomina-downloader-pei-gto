using Microsoft.Extensions.Hosting;
using NominaDownloaderPEIGTO.Application.Commands;
using Serilog;
using Wolverine;

namespace NominaDownloaderPEIGTO.Console.Extensions;

public static class HostBuilderExtensions
{
    public static IHostBuilder ConfigureNominaDownloaderPEIGTO(this IHostBuilder builder)
    {
        return builder
            .UseSerilog()
            .UseWolverine(opts =>
            {
                // Configurar Wolverine para usar los handlers de la aplicación
                opts.Discovery.IncludeAssembly(typeof(StartDownloadSessionCommand).Assembly);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddNominaDownloaderPEIGTOServices();
            });
    }
}
