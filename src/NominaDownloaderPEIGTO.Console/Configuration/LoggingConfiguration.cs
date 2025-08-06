using Serilog;

namespace NominaDownloaderPEIGTO.Console.Configuration;

public static class LoggingConfiguration
{
    public static void ConfigureLogging()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Error()
            // Silenciar logs del sistema y frameworks
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Error)
            .MinimumLevel.Override("Microsoft.Hosting", Serilog.Events.LogEventLevel.Error)
            .MinimumLevel.Override("Microsoft.Extensions", Serilog.Events.LogEventLevel.Error)
            .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Error)
            .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Error)
            .MinimumLevel.Override("Wolverine", Serilog.Events.LogEventLevel.Error)
            .MinimumLevel.Override("OpenTelemetry", Serilog.Events.LogEventLevel.Error)
            .MinimumLevel.Override("System.Net.Http.HttpClient", Serilog.Events.LogEventLevel.Error)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: Path.Combine(AppContext.BaseDirectory, "logs", "recibo-downloader-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Log.Information("🚀 Iniciando NominaDownloader-PEI-GTO...");
    }
}
