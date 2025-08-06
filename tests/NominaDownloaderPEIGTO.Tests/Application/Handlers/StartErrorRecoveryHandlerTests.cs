using FluentAssertions;
using Moq;
using NominaDownloaderPEIGTO.Application.Commands;
using NominaDownloaderPEIGTO.Application.Handlers;
using NominaDownloaderPEIGTO.Application.Interfaces;
using NominaDownloaderPEIGTO.Domain.Entities;
using NominaDownloaderPEIGTO.Domain.Enums;
using NominaDownloaderPEIGTO.Domain.ValueObjects;

namespace NominaDownloaderPEIGTO.Tests.Application.Handlers;

public class StartErrorRecoveryHandlerTests
{
    private readonly Mock<IErrorRecoveryService> _mockRecoveryService;
    private readonly Mock<IWebPortalService> _mockWebPortalService;
    private readonly Mock<IProgressService> _mockProgressService;
    private readonly StartErrorRecoveryHandler _handler;

    public StartErrorRecoveryHandlerTests()
    {
        _mockRecoveryService = new Mock<IErrorRecoveryService>();
        _mockWebPortalService = new Mock<IWebPortalService>();
        _mockProgressService = new Mock<IProgressService>();

        _handler = new StartErrorRecoveryHandler(
            _mockRecoveryService.Object,
            _mockWebPortalService.Object,
            _mockProgressService.Object);
    }

    [Fact]
    public void Constructor_WithNullRecoveryService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new StartErrorRecoveryHandler(
            null!,
            _mockWebPortalService.Object,
            _mockProgressService.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("recoveryService");
    }

    [Fact]
    public void Constructor_WithNullWebPortalService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new StartErrorRecoveryHandler(
            _mockRecoveryService.Object,
            null!,
            _mockProgressService.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("webPortalService");
    }

    [Fact]
    public void Constructor_WithNullProgressService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new StartErrorRecoveryHandler(
            _mockRecoveryService.Object,
            _mockWebPortalService.Object,
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("progressService");
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldStartRecoverySuccessfully()
    {
        // Arrange
        var originalSessionId = Guid.NewGuid();
        var failedPeriods = new List<PeriodInfo>
        {
            new PeriodInfo(2024, 1),
            new PeriodInfo(2024, 2)
        };

        var command = new StartErrorRecoveryCommand(
            originalSessionId,
            failedPeriods,
            @"C:\Downloads",
            3);

        // Usar instancia real en lugar de mock
        var recoverySession = new ErrorRecoverySession(originalSessionId);

        _mockRecoveryService
            .Setup(x => x.CreateRecoverySessionAsync(originalSessionId, failedPeriods, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recoverySession);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.RecoverySessionId.Should().Be(recoverySession.Id);
        result.ProcessedPeriods.Should().BeEquivalentTo(failedPeriods);

        _mockRecoveryService.Verify(
            x => x.CreateRecoverySessionAsync(originalSessionId, failedPeriods, It.IsAny<CancellationToken>()), 
            Times.Once);

        _mockRecoveryService.Verify(
            x => x.CleanupFailedFoldersAsync(failedPeriods, @"C:\Downloads", It.IsAny<CancellationToken>()), 
            Times.Once);

        _mockRecoveryService.Verify(
            x => x.SaveRecoverySessionAsync(recoverySession, It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRecoveryServiceThrows_ShouldReturnFailureResult()
    {
        // Arrange
        var originalSessionId = Guid.NewGuid();
        var failedPeriods = new List<PeriodInfo> { new PeriodInfo(2024, 1) };

        var command = new StartErrorRecoveryCommand(
            originalSessionId,
            failedPeriods,
            @"C:\Downloads",
            3);

        _mockRecoveryService
            .Setup(x => x.CreateRecoverySessionAsync(originalSessionId, failedPeriods, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Recovery service error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.RecoverySessionId.Should().Be(Guid.Empty);
        result.ErrorMessage.Should().Be("Recovery service error");

        _mockRecoveryService.Verify(
            x => x.CreateRecoverySessionAsync(originalSessionId, failedPeriods, It.IsAny<CancellationToken>()), 
            Times.Once);

        // No debería intentar limpiar carpetas o guardar la sesión después del error
        _mockRecoveryService.Verify(
            x => x.CleanupFailedFoldersAsync(It.IsAny<List<PeriodInfo>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), 
            Times.Never);

        _mockRecoveryService.Verify(
            x => x.SaveRecoverySessionAsync(It.IsAny<ErrorRecoverySession>(), It.IsAny<CancellationToken>()), 
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithEmptyFailedPeriods_ShouldHandleCorrectly()
    {
        // Arrange
        var originalSessionId = Guid.NewGuid();
        var emptyFailedPeriods = new List<PeriodInfo>();

        var command = new StartErrorRecoveryCommand(
            originalSessionId,
            emptyFailedPeriods,
            @"C:\Downloads",
            3);

        var recoverySession = new ErrorRecoverySession(originalSessionId);

        _mockRecoveryService
            .Setup(x => x.CreateRecoverySessionAsync(originalSessionId, emptyFailedPeriods, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recoverySession);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.RecoverySessionId.Should().Be(recoverySession.Id);
        result.ProcessedPeriods.Should().BeEmpty();
        result.SuccessfulPeriods.Should().BeEmpty();
        result.StillFailedPeriods.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithMaxRetryAttempts_ShouldRespectRetryLimit()
    {
        // Arrange
        var originalSessionId = Guid.NewGuid();
        var failedPeriods = new List<PeriodInfo> { new PeriodInfo(2024, 1) };

        var command = new StartErrorRecoveryCommand(
            originalSessionId,
            failedPeriods,
            @"C:\Downloads",
            1); // Límite de 1 intento

        var recoverySession = new ErrorRecoverySession(originalSessionId, 1);

        _mockRecoveryService
            .Setup(x => x.CreateRecoverySessionAsync(originalSessionId, failedPeriods, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recoverySession);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.RecoverySessionId.Should().Be(recoverySession.Id);

        // Verificar que se respetó el límite de reintentos
        recoverySession.MaxRetryAttempts.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldNotifyProgressDuringRecovery()
    {
        // Arrange
        var originalSessionId = Guid.NewGuid();
        var failedPeriods = new List<PeriodInfo> { new PeriodInfo(2024, 1) };

        var command = new StartErrorRecoveryCommand(
            originalSessionId,
            failedPeriods,
            @"C:\Downloads",
            3);

        var recoverySession = new ErrorRecoverySession(originalSessionId);

        _mockRecoveryService
            .Setup(x => x.CreateRecoverySessionAsync(originalSessionId, failedPeriods, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recoverySession);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockProgressService.Verify(
            x => x.NotifyMessageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), 
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassTokenToAllServices()
    {
        // Arrange
        var originalSessionId = Guid.NewGuid();
        var failedPeriods = new List<PeriodInfo> { new PeriodInfo(2024, 1) };
        var cancellationToken = new CancellationTokenSource().Token;

        var command = new StartErrorRecoveryCommand(
            originalSessionId,
            failedPeriods,
            @"C:\Downloads",
            3);

        var recoverySession = new ErrorRecoverySession(originalSessionId);

        _mockRecoveryService
            .Setup(x => x.CreateRecoverySessionAsync(originalSessionId, failedPeriods, cancellationToken))
            .ReturnsAsync(recoverySession);

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        _mockRecoveryService.Verify(
            x => x.CreateRecoverySessionAsync(originalSessionId, failedPeriods, cancellationToken), 
            Times.Once);

        _mockRecoveryService.Verify(
            x => x.CleanupFailedFoldersAsync(failedPeriods, @"C:\Downloads", cancellationToken), 
            Times.Once);

        _mockRecoveryService.Verify(
            x => x.SaveRecoverySessionAsync(recoverySession, cancellationToken), 
            Times.Once);

        _mockProgressService.Verify(
            x => x.NotifyMessageAsync(It.IsAny<string>(), cancellationToken), 
            Times.AtLeastOnce);
    }
}
