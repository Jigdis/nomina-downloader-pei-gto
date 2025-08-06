using NominaDownloaderPEIGTO.Application.Interfaces;
using NominaDownloaderPEIGTO.Domain.Entities;

namespace NominaDownloaderPEIGTO.Infrastructure.Services
{
    /// <summary>
    /// Implementación del servicio de progreso que muestra información en consola
    /// </summary>
    public class ConsoleProgressService : IProgressService
    {
        private readonly TextWriter _output;

        public ConsoleProgressService(TextWriter? output = null)
        {
            _output = output ?? Console.Out;
        }

        public async Task NotifySessionStartedAsync(DownloadSession session, CancellationToken cancellationToken = default)
        {
            await _output.WriteLineAsync();
            await _output.WriteLineAsync("=".PadRight(60, '='));
            await _output.WriteLineAsync($"SESIÓN INICIADA: {session.Id}");
            await _output.WriteLineAsync($"Usuario: {session.Credentials.Username}");
            await _output.WriteLineAsync($"Tareas programadas: {session.TotalTasks}");
            await _output.WriteLineAsync($"Navegadores paralelos: {session.Config.MaxParallelBrowsers}");
            await _output.WriteLineAsync($"Directorio: {session.Config.DownloadPath}");
            await _output.WriteLineAsync("=".PadRight(60, '='));
            await _output.WriteLineAsync();
        }

        public async Task NotifyTaskStartedAsync(PeriodTask task, CancellationToken cancellationToken = default)
        {
            await _output.WriteLineAsync($"⏳ Iniciando descarga: {task.Period.DisplayName} (Intento {task.AttemptCount})");
        }

        public async Task NotifyTaskCompletedAsync(PeriodTask task, CancellationToken cancellationToken = default)
        {
            var duration = task.Duration?.TotalSeconds ?? 0;
            await _output.WriteLineAsync($"✅ Completado: {task.Period.DisplayName} ({duration:F1}s) - {task.Files.Count} archivo(s)");
        }

        public async Task NotifyTaskFailedAsync(FailedTask failedTask, CancellationToken cancellationToken = default)
        {
            await _output.WriteLineAsync($"❌ Falló: {failedTask.DisplayMessage}");
        }

        public async Task NotifyFileDownloadedAsync(DownloadedFile file, CancellationToken cancellationToken = default)
        {
            var status = file.IsValid ? "✓" : "⚠";
            var size = FormatFileSize(file.Metadata.FileSize);
            await _output.WriteLineAsync($"   {status} {file.Metadata.FileName} ({size})");
        }

        public async Task NotifySessionCompletedAsync(DownloadSession session, CancellationToken cancellationToken = default)
        {
            await _output.WriteLineAsync();
            await _output.WriteLineAsync("=".PadRight(60, '='));
            await _output.WriteLineAsync($"SESIÓN COMPLETADA: {session.Id}");
            await _output.WriteLineAsync($"Estado: {session.Status}");
            await _output.WriteLineAsync($"Duración: {session.Duration?.ToString(@"hh\:mm\:ss") ?? "N/A"}");
            await _output.WriteLineAsync($"Tareas completadas: {session.CompletedTasks}/{session.TotalTasks}");
            await _output.WriteLineAsync($"Archivos descargados: {session.SuccessfulDownloads}");
            await _output.WriteLineAsync($"Tareas fallidas: {session.FailedTasksCount}");
            await _output.WriteLineAsync($"Progreso: {session.ProgressPercentage:F1}%");
            
            if (!string.IsNullOrEmpty(session.ErrorMessage))
            {
                await _output.WriteLineAsync($"Error: {session.ErrorMessage}");
            }
            
            await _output.WriteLineAsync("=".PadRight(60, '='));
            await _output.WriteLineAsync();
        }

        public async Task NotifyMessageAsync(string message, CancellationToken cancellationToken = default)
        {
            await _output.WriteLineAsync($"💬 {message}");
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024 * 1024):F1} MB";
            return $"{bytes / (1024 * 1024 * 1024):F1} GB";
        }
    }
}
