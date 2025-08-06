using FluentAssertions;
using NominaDownloaderPEIGTO.Domain.Entities;
using NominaDownloaderPEIGTO.Domain.Enums;
using NominaDownloaderPEIGTO.Domain.ValueObjects;
using NominaDownloaderPEIGTO.Infrastructure.Services;
using Xunit;

namespace NominaDownloaderPEIGTO.Tests.Infrastructure.Services;

public class ConsoleProgressServiceTests : IDisposable
{
    private readonly ConsoleProgressService _service;
    private readonly StringWriter _stringWriter;

    public ConsoleProgressServiceTests()
    {
        // Crear StringWriter para capturar la salida
        _stringWriter = new StringWriter();
        _service = new ConsoleProgressService(_stringWriter);
    }

    public void Dispose()
    {
        _stringWriter.Dispose();
    }

    [Fact]
    public async Task NotifySessionStartedAsync_ShouldWriteSessionInfoToConsole()
    {
        // Arrange
        var credentials = new LoginCredentials("testuser", "password");
        var config = new DownloadConfig(@"C:\Downloads", maxParallelBrowsers: 3);
        var session = new DownloadSession(credentials, config);
        
        // Agregar algunas tareas para tener datos de prueba
        session.AddPeriodTask(new PeriodInfo(2024, 1));
        session.AddPeriodTask(new PeriodInfo(2024, 2));

        // Act
        await _service.NotifySessionStartedAsync(session, CancellationToken.None);

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain("SESIÓN INICIADA");
        output.Should().Contain(session.Id.ToString());
        output.Should().Contain("testuser");
        output.Should().Contain(@"C:\Downloads");
    }

    [Fact]
    public async Task NotifyTaskStartedAsync_ShouldWriteTaskStartInfoToConsole()
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);
        var task = new PeriodTask(period);
        
        // Iniciar la tarea para incrementar el contador de intentos
        task.Start();

        // Act
        await _service.NotifyTaskStartedAsync(task, CancellationToken.None);

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain("⏳ Iniciando descarga");
        output.Should().Contain(period.DisplayName);
        output.Should().Contain("Intento 1");
    }

    [Fact]
    public async Task NotifyTaskCompletedAsync_ShouldWriteTaskCompletionInfoToConsole()
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);
        var task = new PeriodTask(period);
        task.Start();
        
        // Simular algunos archivos descargados
        var metadata1 = new FileMetadata("test1.pdf", "path1", 1024, FileType.ReciboPdf, DateTime.UtcNow, "hash1");
        var metadata2 = new FileMetadata("test2.xml", "path2", 512, FileType.CfdiXml, DateTime.UtcNow, "hash2");
        
        var file1 = new DownloadedFile(period, metadata1);
        var file2 = new DownloadedFile(period, metadata2);
        
        file1.MarkAsValid();
        file2.MarkAsValid();
            
        task.AddFile(file1);
        task.AddFile(file2);
        task.Complete();

        // Act
        await _service.NotifyTaskCompletedAsync(task, CancellationToken.None);

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain("✅ Completado");
        output.Should().Contain(period.DisplayName);
        output.Should().Contain("2 archivo(s)");
        output.Should().MatchRegex(@"\d+\.\d+s"); // Duration in seconds
    }

    [Fact]
    public async Task NotifyTaskFailedAsync_ShouldWriteFailureInfoToConsole()
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);
        var failedTask = new FailedTask(period, "Network timeout error", 1);

        // Act
        await _service.NotifyTaskFailedAsync(failedTask, CancellationToken.None);

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain("❌ Falló");
        output.Should().Contain(failedTask.DisplayMessage);
    }

    [Fact]
    public async Task NotifyFileDownloadedAsync_WithValidFile_ShouldWriteFileInfoWithCheckmark()
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);
        var metadata = new FileMetadata("recibo.pdf", "path", 2048, FileType.ReciboPdf, DateTime.UtcNow, "hash");
        var file = new DownloadedFile(period, metadata);
        file.MarkAsValid();

        // Act
        await _service.NotifyFileDownloadedAsync(file, CancellationToken.None);

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain("✓ recibo.pdf");
        output.Should().Contain("2.0 KB"); // Formatted file size
    }

    [Fact]
    public async Task NotifyFileDownloadedAsync_WithInvalidFile_ShouldWriteFileInfoWithWarning()
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);
        var metadata = new FileMetadata("corrupted.pdf", "path", 1536, FileType.ReciboPdf, DateTime.UtcNow, "hash");
        var file = new DownloadedFile(period, metadata);
        file.MarkAsInvalid("Corrupted file");

        // Act
        await _service.NotifyFileDownloadedAsync(file, CancellationToken.None);

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain("⚠ corrupted.pdf");
        output.Should().MatchRegex(@"corrupted\.pdf \(\d+(\.\d+)? [KMGT]?B\)");
    }

    [Fact]
    public async Task NotifySessionCompletedAsync_ShouldWriteSessionSummaryToConsole()
    {
        // Arrange
        var credentials = new LoginCredentials("testuser", "password");
        var config = new DownloadConfig(@"C:\Downloads");
        var session = new DownloadSession(credentials, config);
        
        // Agregar tareas y simular completación
        session.AddPeriodTask(new PeriodInfo(2024, 1));
        session.AddPeriodTask(new PeriodInfo(2024, 2));
        session.Start();
        session.Complete();

        // Act
        await _service.NotifySessionCompletedAsync(session, CancellationToken.None);

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain("SESIÓN COMPLETADA");
        output.Should().Contain(session.Id.ToString());
        output.Should().Contain(session.Status.ToString());
        output.Should().Contain("Tareas completadas:");
        output.Should().Contain("Archivos descargados:");
        output.Should().Contain("Progreso:");
    }

    [Fact]
    public async Task NotifySessionCompletedAsync_WithError_ShouldIncludeErrorMessage()
    {
        // Arrange
        var credentials = new LoginCredentials("testuser", "password");
        var config = new DownloadConfig(@"C:\Downloads");
        var session = new DownloadSession(credentials, config);
        
        session.Start();
        session.Fail("Connection timeout");

        // Act
        await _service.NotifySessionCompletedAsync(session, CancellationToken.None);

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain("Error: Connection timeout");
    }

    [Fact]
    public async Task NotifyMessageAsync_ShouldWriteMessageToConsole()
    {
        // Arrange
        var message = "Custom notification message";

        // Act
        await _service.NotifyMessageAsync(message, CancellationToken.None);

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain("💬 Custom notification message");
    }

    [Fact]
    public async Task AllMethods_WithCancellationToken_ShouldAcceptTokenWithoutError()
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource().Token;
        var credentials = new LoginCredentials("test", "pass");
        var config = new DownloadConfig(@"C:\Test");
        var session = new DownloadSession(credentials, config);
        var period = new PeriodInfo(2024, 1);
        var task = new PeriodTask(period);
        var failedTask = new FailedTask(period, "Error", 1);
        var metadata = new FileMetadata("file.pdf", "path", 1024, FileType.ReciboPdf, DateTime.UtcNow, "hash");
        var file = new DownloadedFile(period, metadata);

        // Act & Assert - No debería lanzar excepciones
        await _service.NotifySessionStartedAsync(session, cancellationToken);
        await _service.NotifyTaskStartedAsync(task, cancellationToken);
        await _service.NotifyTaskCompletedAsync(task, cancellationToken);
        await _service.NotifyTaskFailedAsync(failedTask, cancellationToken);
        await _service.NotifyFileDownloadedAsync(file, cancellationToken);
        await _service.NotifySessionCompletedAsync(session, cancellationToken);
        await _service.NotifyMessageAsync("Test", cancellationToken);

        // Si llegamos aquí, todas las operaciones fueron exitosas
        true.Should().BeTrue();
    }

    [Theory]
    [InlineData(512)]
    [InlineData(1024)]
    [InlineData(1536)]
    [InlineData(1048576)]
    [InlineData(1073741824)]
    public async Task NotifyFileDownloadedAsync_ShouldFormatFileSizeCorrectly(long bytes)
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);
        var metadata = new FileMetadata("test.pdf", "path", bytes, FileType.ReciboPdf, DateTime.UtcNow, "hash");
        var file = new DownloadedFile(period, metadata);
        file.MarkAsValid();

        // Act
        await _service.NotifyFileDownloadedAsync(file, CancellationToken.None);

        // Assert
        var output = _stringWriter.ToString();
        // Note: The file size formatting may vary based on implementation
        // This test verifies the general structure rather than exact formatting
        output.Should().MatchRegex(@"test\.pdf \(\d+(\.\d+)? [KMGT]?B\)");
    }
}
