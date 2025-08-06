using Microsoft.Extensions.DependencyInjection;
using NominaDownloaderPEIGTO.Application.Interfaces;
using NominaDownloaderPEIGTO.Application.Handlers;
using NominaDownloaderPEIGTO.Console.Services;
using NominaDownloaderPEIGTO.Domain.ValueObjects;
using NominaDownloaderPEIGTO.Infrastructure.Repositories;
using NominaDownloaderPEIGTO.Infrastructure.Services;

namespace NominaDownloaderPEIGTO.Console.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNominaDownloaderPEIGTOServices(this IServiceCollection services)
    {
        // Registrar configuración por defecto
        services.AddSingleton<DownloadConfig>(sp => new DownloadConfig(
            downloadPath: @"C:\Recibos",
            maxParallelBrowsers: 16,
            timeoutPerDownload: TimeSpan.FromMinutes(30)
        ));

        // Registrar servicios de la infraestructura
        services.AddSingleton<IDownloadSessionRepository, InMemoryDownloadSessionRepository>();
        services.AddSingleton<IFileValidationService, FileValidationService>();
        services.AddSingleton<IProgressService, ConsoleProgressService>();
        
        // Registrar nuevos servicios de snapshot y recuperación de errores
        services.AddSingleton<IDownloadSnapshotService, DownloadSnapshotService>();
        services.AddSingleton<IErrorRecoveryService, ErrorRecoveryService>();
        
        // Registrar handlers
        services.AddScoped<StartDownloadSessionHandler>();
        services.AddScoped<CreateSnapshotHandler>();
        services.AddScoped<AnalyzeEmptyFoldersHandler>();
        services.AddScoped<StartErrorRecoveryHandler>();
        services.AddScoped<GetAvailableYearsHandler>();
        services.AddScoped<GetAvailablePeriodsHandler>();
        
        // En desarrollo, usar un servicio mock en lugar del real
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            System.Console.WriteLine("🔧 Modo desarrollo: usando servicios mock");
            // services.AddSingleton<IWebPortalService, MockWebPortalService>();
        }
        else
        {
            services.AddSingleton<IWebPortalService, SeleniumWebPortalService>();
        }
        
        services.AddTransient<IParallelDownloadService, ParallelDownloadService>();
        
        // Registrar servicios de la consola
        services.AddSingleton<IUserInteractionService, ConsoleUserInteractionService>();
        services.AddSingleton<ApplicationRunner>();

        return services;
    }
}
