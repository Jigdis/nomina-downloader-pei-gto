using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NominaDownloaderPEIGTO.Console.Configuration;
using NominaDownloaderPEIGTO.Console.Extensions;
using NominaDownloaderPEIGTO.Console.Services;
using Serilog;

namespace NominaDownloaderPEIGTO.Console;

class Program
{
    static async Task Main(string[] args)
    {
        LoggingConfiguration.ConfigureLogging();

        try
        {
            System.Console.WriteLine("🚀 NominaDownloader-PEI-GTO v1.0");
            System.Console.WriteLine("📸 Descarga automatizada de recibos de nómina del Portal PEI Guanajuato");
            System.Console.WriteLine("════════════════════════════════════════════════════════════════");

            var host = Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging =>
                {
                    // Limpiar todos los proveedores de logging por defecto
                    logging.ClearProviders();
                    // Agregar solo Serilog
                    logging.AddSerilog();
                })
                .ConfigureNominaDownloaderPEIGTO()
                .Build();

            await host.StartAsync();

            var applicationRunner = host.Services.GetRequiredService<ApplicationRunner>();
            await applicationRunner.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Error fatal en la aplicación");
            System.Console.WriteLine($"💥 Error fatal: {ex.Message}");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
