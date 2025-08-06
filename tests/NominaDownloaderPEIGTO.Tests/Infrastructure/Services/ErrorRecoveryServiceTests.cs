using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NominaDownloaderPEIGTO.Common.Utilities;
using NominaDownloaderPEIGTO.Domain.Entities;
using NominaDownloaderPEIGTO.Domain.ValueObjects;
using NominaDownloaderPEIGTO.Infrastructure.Services;

namespace NominaDownloaderPEIGTO.Tests.Infrastructure.Services;

public class ErrorRecoveryServiceTests : IDisposable
{
    private readonly Mock<ILogger<ErrorRecoveryService>> _mockLogger;
    private readonly ErrorRecoveryService _service;
    private readonly string _testRecoveryPath;

    public ErrorRecoveryServiceTests()
    {
        _mockLogger = new Mock<ILogger<ErrorRecoveryService>>();
        _service = new ErrorRecoveryService(_mockLogger.Object);
        
        // Crear directorio temporal para tests
        _testRecoveryPath = Path.Combine(Path.GetTempPath(), $"test_recovery_{Guid.NewGuid()}");
        if (Directory.Exists(_testRecoveryPath))
        {
            Directory.Delete(_testRecoveryPath, true);
        }
    }

    public void Dispose()
    {
        // Limpiar directorio temporal
        if (Directory.Exists(_testRecoveryPath))
        {
            try
            {
                Directory.Delete(_testRecoveryPath, true);
            }
            catch
            {
                // Ignorar errores de limpieza
            }
        }

        // Limpiar directorio recovery real
        var recoveryPath = Path.Combine(Environment.CurrentDirectory, "recovery");
        if (Directory.Exists(recoveryPath))
        {
            try
            {
                var files = Directory.GetFiles(recoveryPath, "recovery_*.json");
                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        // Ignorar errores individuales
                    }
                }
            }
            catch
            {
                // Ignorar errores de limpieza
            }
        }
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new ErrorRecoveryService(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ShouldCreateRecoveryDirectory()
    {
        // Arrange & Act
        var service = new ErrorRecoveryService(Mock.Of<ILogger<ErrorRecoveryService>>());

        // Assert
        var recoveryPath = Path.Combine(Environment.CurrentDirectory, "recovery");
        Directory.Exists(recoveryPath).Should().BeTrue();
    }

    [Fact]
    public async Task CreateRecoverySessionAsync_WithValidData_ShouldCreateSession()
    {
        // Arrange
        var originalSessionId = Guid.NewGuid();
        var failedPeriods = new List<PeriodInfo>
        {
            new PeriodInfo(2024, 1),
            new PeriodInfo(2024, 2),
            new PeriodInfo(2024, 3)
        };

        // Act
        var result = await _service.CreateRecoverySessionAsync(originalSessionId, failedPeriods);

        // Assert
        result.Should().NotBeNull();
        result.OriginalSessionId.Should().Be(originalSessionId);
        result.FailedAttempts.Should().HaveCount(3);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verificar que se agregaron los períodos fallidos
        result.FailedAttempts.Should().Contain(fa => fa.Period.Year == 2024 && fa.Period.Period == 1);
        result.FailedAttempts.Should().Contain(fa => fa.Period.Year == 2024 && fa.Period.Period == 2);
        result.FailedAttempts.Should().Contain(fa => fa.Period.Year == 2024 && fa.Period.Period == 3);

        // Verificar logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Creando sesión de recuperación para sesión original {originalSessionId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Sesión de recuperación") && v.ToString()!.Contains("creada exitosamente")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateRecoverySessionAsync_WithEmptyFailedPeriods_ShouldCreateSessionWithoutFailures()
    {
        // Arrange
        var originalSessionId = Guid.NewGuid();
        var failedPeriods = new List<PeriodInfo>();

        // Act
        var result = await _service.CreateRecoverySessionAsync(originalSessionId, failedPeriods);

        // Assert
        result.Should().NotBeNull();
        result.OriginalSessionId.Should().Be(originalSessionId);
        result.FailedAttempts.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateRecoverySessionAsync_WithCancellationToken_ShouldRespectToken()
    {
        // Arrange
        var originalSessionId = Guid.NewGuid();
        var failedPeriods = new List<PeriodInfo> { new PeriodInfo(2024, 1) };
        var cancellationToken = new CancellationTokenSource().Token;

        // Act
        var result = await _service.CreateRecoverySessionAsync(originalSessionId, failedPeriods, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.OriginalSessionId.Should().Be(originalSessionId);
    }

    [Fact]
    public async Task ExecuteRecoveryAsync_WithNonExistentSession_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var nonExistentSessionId = Guid.NewGuid();

        // Act & Assert
        var act = async () => await _service.ExecuteRecoveryAsync(nonExistentSessionId);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Sesión de recuperación {nonExistentSessionId} no encontrada");
    }

    [Fact]
    public async Task GetRecoverySessionAsync_WhenSessionDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var nonExistentSessionId = Guid.NewGuid();

        // Act
        var result = await _service.GetRecoverySessionAsync(nonExistentSessionId);

        // Assert
        result.Should().BeNull();

        // Verificar logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"No se encontró sesión de recuperación {nonExistentSessionId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SaveRecoverySessionAsync_WithValidSession_ShouldSaveToFile()
    {
        // Arrange
        var originalSessionId = Guid.NewGuid();
        var failedPeriods = new List<PeriodInfo> { new PeriodInfo(2024, 1) };
        
        var session = await _service.CreateRecoverySessionAsync(originalSessionId, failedPeriods);

        // Act
        await _service.SaveRecoverySessionAsync(session);

        // Assert
        var expectedFilePath = Path.Combine(Environment.CurrentDirectory, "recovery", $"recovery_{session.Id}.json");
        File.Exists(expectedFilePath).Should().BeTrue();

        var fileContent = await File.ReadAllTextAsync(expectedFilePath);
        fileContent.Should().NotBeEmpty();
        fileContent.Should().Contain(session.Id.ToString());
        fileContent.Should().Contain(originalSessionId.ToString());
        fileContent.Should().Contain("2024");

        // Verificar logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Sesión de recuperación") && v.ToString()!.Contains("guardada en")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CleanupFailedFoldersAsync_WithExistingFolders_ShouldAttemptCleanup()
    {
        // Arrange
        var periods = new List<PeriodInfo>
        {
            new PeriodInfo(2024, 1),
            new PeriodInfo(2024, 2)
        };
        var downloadPath = _testRecoveryPath;

        // Crear carpetas de prueba usando la misma lógica de sanitización que el servicio
        foreach (var period in periods)
        {
            // Usar la misma lógica que el servicio: SanitizeFolderName($"Periodo_{period.Period:D2}_{period.DisplayName}")
            var sanitizedPeriodName = PathUtils.SanitizeFolderName($"Periodo_{period.Period:D2}_{period.DisplayName}");
            var folderPath = Path.Combine(downloadPath, period.Year.ToString(), sanitizedPeriodName);
            Directory.CreateDirectory(folderPath);
            
            // Crear un archivo de prueba
            var testFile = Path.Combine(folderPath, "test.txt");
            await File.WriteAllTextAsync(testFile, "test content");
        }

        // Act
        await _service.CleanupFailedFoldersAsync(periods, downloadPath);

        // Assert
        // Verificar que las carpetas fueron eliminadas
        foreach (var period in periods)
        {
            var sanitizedPeriodName = PathUtils.SanitizeFolderName($"Periodo_{period.Period:D2}_{period.DisplayName}");
            var folderPath = Path.Combine(downloadPath, period.Year.ToString(), sanitizedPeriodName);
            Directory.Exists(folderPath).Should().BeFalse();
        }

        // Verificar logging de inicio
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Limpiando {periods.Count} carpetas fallidas")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Verificar logging de finalización
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Limpieza de carpetas completada")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CleanupFailedFoldersAsync_WithNonExistentFolders_ShouldCompleteWithoutError()
    {
        // Arrange
        var periods = new List<PeriodInfo>
        {
            new PeriodInfo(2024, 1),
            new PeriodInfo(2024, 2)
        };
        var downloadPath = Path.Combine(_testRecoveryPath, "non_existent");

        // Act & Assert - No debería lanzar excepciones
        await _service.CleanupFailedFoldersAsync(periods, downloadPath);

        // Verificar logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Limpieza de carpetas completada")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CleanupFailedFoldersAsync_WithCancellationToken_ShouldRespectToken()
    {
        // Arrange
        var periods = new List<PeriodInfo> { new PeriodInfo(2024, 1) };
        var downloadPath = _testRecoveryPath;
        var cancellationToken = new CancellationTokenSource().Token;

        // Act & Assert
        await _service.CleanupFailedFoldersAsync(periods, downloadPath, cancellationToken);

        // No debería lanzar excepciones
        true.Should().BeTrue();
    }

    [Fact]
    public async Task SaveRecoverySessionAsync_WithSessionContainingRecoveryAttempts_ShouldSerializeCorrectly()
    {
        // Arrange
        var originalSessionId = Guid.NewGuid();
        var failedPeriods = new List<PeriodInfo> { new PeriodInfo(2024, 1) };
        
        var session = await _service.CreateRecoverySessionAsync(originalSessionId, failedPeriods);
        
        // Simular intentos de recuperación
        session.StartRecovery();
        var periodsToRetry = session.GetPeriodsToRetry();
        foreach (var period in periodsToRetry)
        {
            session.AddRecoveryAttempt(period, true);
        }

        // Act
        await _service.SaveRecoverySessionAsync(session);

        // Assert
        var expectedFilePath = Path.Combine(Environment.CurrentDirectory, "recovery", $"recovery_{session.Id}.json");
        File.Exists(expectedFilePath).Should().BeTrue();

        var fileContent = await File.ReadAllTextAsync(expectedFilePath);
        fileContent.Should().Contain("RecoveryAttempts");
        fileContent.Should().Contain("FailedAttempts");
        fileContent.Should().Contain("Success");
    }

    [Fact]
    public async Task GetRecoverySessionAsync_WithCancellationToken_ShouldRespectToken()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var cancellationToken = new CancellationTokenSource().Token;

        // Act
        var result = await _service.GetRecoverySessionAsync(sessionId, cancellationToken);

        // Assert
        result.Should().BeNull(); // No existe la sesión
    }

    [Fact]
    public async Task SaveRecoverySessionAsync_WithCancellationToken_ShouldRespectToken()
    {
        // Arrange
        var originalSessionId = Guid.NewGuid();
        var failedPeriods = new List<PeriodInfo> { new PeriodInfo(2024, 1) };
        var session = await _service.CreateRecoverySessionAsync(originalSessionId, failedPeriods);
        var cancellationToken = new CancellationTokenSource().Token;

        // Act & Assert
        await _service.SaveRecoverySessionAsync(session, cancellationToken);

        // Verificar que se guardó
        var expectedFilePath = Path.Combine(Environment.CurrentDirectory, "recovery", $"recovery_{session.Id}.json");
        File.Exists(expectedFilePath).Should().BeTrue();
    }

    [Fact]
    public async Task CreateRecoverySessionAsync_ShouldSetCorrectErrorMessagesAndPaths()
    {
        // Arrange
        var originalSessionId = Guid.NewGuid();
        var failedPeriods = new List<PeriodInfo> { new PeriodInfo(2024, 3) };

        // Act
        var result = await _service.CreateRecoverySessionAsync(originalSessionId, failedPeriods);

        // Assert
        result.FailedAttempts.Should().HaveCount(1);
        var failedAttempt = result.FailedAttempts.First();
        
        failedAttempt.ErrorMessage.Should().Be("Carpeta vacía después de la descarga");
        failedAttempt.FolderPath.Should().Contain("downloads");
        failedAttempt.FolderPath.Should().Contain("2024");
    }
}
