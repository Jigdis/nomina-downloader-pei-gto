using Microsoft.Extensions.Logging;
using NominaDownloaderPEIGTO.Application.Commands;
using NominaDownloaderPEIGTO.Application.Handlers;
using NominaDownloaderPEIGTO.Application.Interfaces;
using NominaDownloaderPEIGTO.Application.Queries;
using NominaDownloaderPEIGTO.Domain.ValueObjects;
using System.Globalization;

namespace NominaDownloaderPEIGTO.Console.Services
{
    /// <summary>
    /// Servicio de aplicación con soporte para snapshots y recuperación de errores
    /// </summary>
    public class ApplicationRunner : IApplicationRunner
    {
        private readonly ILogger<ApplicationRunner> _logger;
        private readonly StartDownloadSessionHandler _downloadHandler;
        private readonly CreateSnapshotHandler _snapshotHandler;
        private readonly AnalyzeEmptyFoldersHandler _analyzeHandler;
        private readonly StartErrorRecoveryHandler _recoveryHandler;
        private readonly GetAvailableYearsHandler _yearsHandler;
        private readonly GetAvailablePeriodsHandler _periodsHandler;

        public ApplicationRunner(
            ILogger<ApplicationRunner> logger,
            StartDownloadSessionHandler downloadHandler,
            CreateSnapshotHandler snapshotHandler,
            AnalyzeEmptyFoldersHandler analyzeHandler,
            StartErrorRecoveryHandler recoveryHandler,
            GetAvailableYearsHandler yearsHandler,
            GetAvailablePeriodsHandler periodsHandler)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _downloadHandler = downloadHandler ?? throw new ArgumentNullException(nameof(downloadHandler));
            _snapshotHandler = snapshotHandler ?? throw new ArgumentNullException(nameof(snapshotHandler));
            _analyzeHandler = analyzeHandler ?? throw new ArgumentNullException(nameof(analyzeHandler));
            _recoveryHandler = recoveryHandler ?? throw new ArgumentNullException(nameof(recoveryHandler));
            _yearsHandler = yearsHandler ?? throw new ArgumentNullException(nameof(yearsHandler));
            _periodsHandler = periodsHandler ?? throw new ArgumentNullException(nameof(periodsHandler));
        }

        public async Task RunAsync()
        {
            try
            {
                _logger.LogInformation("🚀 Iniciando NominaDownloader-PEI-GTO v1.0");
                
                // Obtener configuración del usuario
                var config = await GetUserConfigurationAsync();
                
                // Generar un ID de sesión único para todo el proceso
                var sessionId = Guid.NewGuid();
                
                // Crear snapshot inicial
                var snapshotResult = await CreateInitialSnapshotAsync(sessionId, config);
                if (!snapshotResult.Success)
                {
                    System.Console.WriteLine($"❌ Error al crear snapshot inicial: {snapshotResult.ErrorMessage}");
                    return;
                }

                System.Console.WriteLine("📸 Snapshot inicial creado exitosamente");

                // Ejecutar descarga principal
                var downloadResult = await ExecuteMainDownloadAsync(config);
                
                if (downloadResult.Success)
                {
                    System.Console.WriteLine("✅ Descarga principal completada");
                    
                    // Analizar carpetas vacías usando el mismo sessionId
                    await AnalyzeAndRecoverFailuresAsync(downloadResult.SessionId, config);
                }
                else
                {
                    System.Console.WriteLine($"❌ Error en descarga principal: {downloadResult.ErrorMessage}");
                }

                System.Console.WriteLine("\n🎉 Proceso completado. Presiona cualquier tecla para salir...");
                System.Console.ReadKey();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en la ejecución de la aplicación");
                System.Console.WriteLine($"💥 Error inesperado: {ex.Message}");
            }
        }

        private async Task<DownloadConfiguration> GetUserConfigurationAsync()
        {
            System.Console.WriteLine("=== CONFIGURACIÓN DE DESCARGA ===");
            
            // Obtener credenciales
            System.Console.Write("� RFC: ");
            var rfc = System.Console.ReadLine() ?? "";
            
            System.Console.Write("🔐 Contraseña: ");
            var password = ReadPasswordFromConsole();
            
            var credentials = new LoginCredentials(rfc, password);
            
            // Obtener configuración de descarga
            System.Console.Write("\n📁 Ruta de descarga (Enter para usar 'C:\\Recibos'): ");
            var downloadPath = System.Console.ReadLine();
            if (string.IsNullOrEmpty(downloadPath))
            {
                downloadPath = @"C:\Recibos";
            }

            var downloadConfig = new DownloadConfig(downloadPath, maxParallelBrowsers: 16);

            // Obtener períodos a descargar
            var periods = await GetPeriodsToDownloadAsync(credentials);

            return new DownloadConfiguration(credentials, downloadConfig, periods);
        }

        private async Task<List<PeriodInfo>> GetPeriodsToDownloadAsync(LoginCredentials credentials)
        {
            System.Console.WriteLine("\n=== SELECCIÓN DE PERÍODOS ===");
            System.Console.WriteLine("1. Todos los años disponibles");
            System.Console.WriteLine("2. Año específico");
            System.Console.WriteLine("3. Período específico");
            
            System.Console.Write("Selecciona una opción (1-3): ");
            var option = System.Console.ReadLine();

            return option switch
            {
                "1" => await GetAllAvailablePeriodsWithCredentialsAsync(credentials),
                "2" => await GetPeriodsForSpecificYearAsync(credentials),
                "3" => await GetSpecificPeriodAsync(credentials),
                _ => new List<PeriodInfo>()
            };
        }

        private async Task<List<PeriodInfo>> GetPeriodsForSpecificYearAsync(LoginCredentials credentials)
        {
            // Primero obtener la lista de años disponibles
            var yearsResult = await _yearsHandler.Handle(new GetAvailableYearsQuery(credentials));
            if (!yearsResult.Success || !yearsResult.Years.Any())
            {
                System.Console.WriteLine($"❌ No se pudieron obtener los años disponibles: {yearsResult.ErrorMessage}");
                return new List<PeriodInfo>();
            }

            var availableYears = yearsResult.Years.ToList();
            System.Console.WriteLine("\n📅 Años disponibles:");
            for (int i = 0; i < availableYears.Count; i++)
            {
                System.Console.WriteLine($"{i + 1}. {availableYears[i]}");
            }

            System.Console.Write($"\nSelecciona un año (1-{availableYears.Count}): ");
            if (int.TryParse(System.Console.ReadLine(), out int selection) && 
                selection >= 1 && selection <= availableYears.Count)
            {
                var selectedYear = availableYears[selection - 1];
                var periodsResult = await _periodsHandler.Handle(new GetAvailablePeriodsQuery(credentials, selectedYear));
                if (periodsResult.Success && periodsResult.Periods.Any())
                {
                    var periods = periodsResult.Periods.ToList();
                    System.Console.WriteLine($"📅 Se seleccionaron {periods.Count()} períodos para el año {selectedYear}");
                    return periods;
                }
                else
                {
                    System.Console.WriteLine($"❌ No se pudieron obtener períodos para el año {selectedYear}: {periodsResult.ErrorMessage}");
                }
            }
            else
            {
                System.Console.WriteLine("❌ Selección inválida");
            }
            
            return new List<PeriodInfo>();
        }

        private async Task<List<PeriodInfo>> GetSpecificPeriodAsync(LoginCredentials credentials)
        {
            // Primero obtener la lista de años disponibles
            var yearsResult = await _yearsHandler.Handle(new GetAvailableYearsQuery(credentials));
            if (!yearsResult.Success || !yearsResult.Years.Any())
            {
                System.Console.WriteLine($"❌ No se pudieron obtener los años disponibles: {yearsResult.ErrorMessage}");
                return new List<PeriodInfo>();
            }

            var availableYears = yearsResult.Years.ToList();
            System.Console.WriteLine("\n📅 Años disponibles:");
            for (int i = 0; i < availableYears.Count; i++)
            {
                System.Console.WriteLine($"{i + 1}. {availableYears[i]}");
            }

            System.Console.Write($"\nSelecciona un año (1-{availableYears.Count}): ");
            if (int.TryParse(System.Console.ReadLine(), out int yearSelection) && 
                yearSelection >= 1 && yearSelection <= availableYears.Count)
            {
                var selectedYear = availableYears[yearSelection - 1];
                
                // Obtener los períodos disponibles para el año seleccionado
                var periodsResult = await _periodsHandler.Handle(new GetAvailablePeriodsQuery(credentials, selectedYear));
                if (!periodsResult.Success || !periodsResult.Periods.Any())
                {
                    System.Console.WriteLine($"❌ No se pudieron obtener períodos para el año {selectedYear}: {periodsResult.ErrorMessage}");
                    return new List<PeriodInfo>();
                }

                var availablePeriods = periodsResult.Periods.ToList();
                System.Console.WriteLine($"\n📅 Períodos disponibles para {selectedYear}:");
                for (int i = 0; i < availablePeriods.Count; i++)
                {
                    System.Console.WriteLine($"{i + 1}. {availablePeriods[i].DisplayName}");
                }

                System.Console.Write($"\nSelecciona un período (1-{availablePeriods.Count}): ");
                if (int.TryParse(System.Console.ReadLine(), out int periodSelection) && 
                    periodSelection >= 1 && periodSelection <= availablePeriods.Count)
                {
                    var selectedPeriod = availablePeriods[periodSelection - 1];
                    System.Console.WriteLine($"📅 Se seleccionó: {selectedPeriod.DisplayName}");
                    return new List<PeriodInfo> { selectedPeriod };
                }
                else
                {
                    System.Console.WriteLine("❌ Selección de período inválida");
                }
            }
            else
            {
                System.Console.WriteLine("❌ Selección de año inválida");
            }
            
            return new List<PeriodInfo>();
        }

        private async Task<CreateSnapshotResult> CreateInitialSnapshotAsync(Guid sessionId, DownloadConfiguration config)
        {
            var command = new CreateSnapshotCommand(sessionId, config.Periods, config.DownloadConfig.DownloadPath);
            
            return await _snapshotHandler.Handle(command);
        }

        private async Task<(bool Success, Guid SessionId, string? ErrorMessage)> ExecuteMainDownloadAsync(DownloadConfiguration config)
        {
            try
            {
                var command = new StartDownloadSessionCommand(config.Credentials, config.DownloadConfig, config.Periods);
                var result = await _downloadHandler.Handle(command);
                
                return (result.Success, result.SessionId, result.ErrorMessage);
            }
            catch (Exception ex)
            {
                return (false, Guid.Empty, ex.Message);
            }
        }

        private async Task AnalyzeAndRecoverFailuresAsync(Guid sessionId, DownloadConfiguration config)
        {
            System.Console.WriteLine("\n🔍 Analizando carpetas vacías...");
            
            var analyzeQuery = new AnalyzeEmptyFoldersQuery(sessionId);
            var analyzeResult = await _analyzeHandler.Handle(analyzeQuery);

            if (analyzeResult.HasEmptyFolders)
            {
                System.Console.WriteLine($"⚠️  Se encontraron {analyzeResult.FailedPeriods.Count} períodos con carpetas vacías:");
                
                foreach (var period in analyzeResult.FailedPeriods)
                {
                    System.Console.WriteLine($"   • {period.DisplayName}");
                }

                System.Console.Write("\n🔄 ¿Deseas intentar recuperar las descargas fallidas? (s/n): ");
                var response = System.Console.ReadLine()?.ToLower();

                if (response == "s" || response == "sí" || response == "si")
                {
                    await ExecuteErrorRecoveryAsync(sessionId, analyzeResult.FailedPeriods, config.DownloadConfig.DownloadPath);
                }
            }
            else
            {
                System.Console.WriteLine("✅ Todas las carpetas tienen contenido. No se requiere recuperación.");
            }
        }

        private async Task ExecuteErrorRecoveryAsync(Guid sessionId, List<PeriodInfo> failedPeriods, string downloadPath)
        {
            try
            {
                System.Console.WriteLine("\n🚑 Iniciando proceso de recuperación de errores...");
                
                var recoveryCommand = new StartErrorRecoveryCommand(sessionId, failedPeriods, downloadPath);
                var recoveryResult = await _recoveryHandler.Handle(recoveryCommand);

                if (recoveryResult.Success)
                {
                    System.Console.WriteLine("✅ Proceso de recuperación completado:");
                    System.Console.WriteLine($"   • Períodos procesados: {recoveryResult.ProcessedPeriods?.Count ?? 0}");
                    System.Console.WriteLine($"   • Recuperados exitosamente: {recoveryResult.SuccessfulPeriods?.Count ?? 0}");
                    System.Console.WriteLine($"   • Aún fallidos: {recoveryResult.StillFailedPeriods?.Count ?? 0}");

                    if (recoveryResult.StillFailedPeriods?.Any() == true)
                    {
                        System.Console.WriteLine("\n⚠️  Períodos que aún fallan:");
                        foreach (var period in recoveryResult.StillFailedPeriods)
                        {
                            System.Console.WriteLine($"   • {period.DisplayName}");
                        }
                    }
                }
                else
                {
                    System.Console.WriteLine($"❌ Error en el proceso de recuperación: {recoveryResult.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"💥 Error durante la recuperación: {ex.Message}");
                _logger.LogError(ex, "Error durante la recuperación de errores");
            }
        }

        private string ReadPasswordFromConsole()
        {
            var password = "";
            ConsoleKeyInfo key;
            
            do
            {
                key = System.Console.ReadKey(true);
                
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    password += key.KeyChar;
                    System.Console.Write("*");
                }
                else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password = password[0..^1];
                    System.Console.Write("\b \b");
                }
            }
            while (key.Key != ConsoleKey.Enter);
            
            System.Console.WriteLine();
            return password;
        }

        private async Task<List<PeriodInfo>> GetAllAvailablePeriodsWithCredentialsAsync(LoginCredentials credentials)
        {
            // Obtener todos los años disponibles
            var yearsResult = await _yearsHandler.Handle(new GetAvailableYearsQuery(credentials));
            if (!yearsResult.Success || !yearsResult.Years.Any())
            {
                System.Console.WriteLine($"❌ No se pudieron obtener los años disponibles: {yearsResult.ErrorMessage}");
                return new List<PeriodInfo>();
            }

            var allPeriods = new List<PeriodInfo>();
            var availableYears = yearsResult.Years.ToList();

            System.Console.WriteLine($"📅 Obteniendo períodos para {availableYears.Count} años...");

            foreach (var year in availableYears)
            {
                var periodsResult = await _periodsHandler.Handle(new GetAvailablePeriodsQuery(credentials, year));
                if (periodsResult.Success && periodsResult.Periods.Any())
                {
                    allPeriods.AddRange(periodsResult.Periods);
                    System.Console.WriteLine($"   ✅ Año {year}: {periodsResult.Periods.Count()} períodos");
                }
                else
                {
                    System.Console.WriteLine($"   ⚠️  Año {year}: Sin períodos disponibles");
                }
            }

            System.Console.WriteLine($"📅 Total: {allPeriods.Count} períodos disponibles");
            return allPeriods;
        }
    }

    /// <summary>
    /// Configuración para la descarga
    /// </summary>
    public record DownloadConfiguration(
        LoginCredentials Credentials,
        DownloadConfig DownloadConfig,
        List<PeriodInfo> Periods
    );
}
