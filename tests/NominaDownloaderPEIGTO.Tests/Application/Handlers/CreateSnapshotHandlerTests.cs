using FluentAssertions;
using Moq;
using NominaDownloaderPEIGTO.Application.Commands;
using NominaDownloaderPEIGTO.Application.Handlers;
using NominaDownloaderPEIGTO.Application.Interfaces;
using NominaDownloaderPEIGTO.Domain.Entities;
using NominaDownloaderPEIGTO.Domain.ValueObjects;

namespace NominaDownloaderPEIGTO.Tests.Application.Handlers;

public class CreateSnapshotHandlerTests
{
    private readonly Mock<IDownloadSnapshotService> _mockSnapshotService;
    private readonly CreateSnapshotHandler _handler;

    public CreateSnapshotHandlerTests()
    {
        _mockSnapshotService = new Mock<IDownloadSnapshotService>();
        _handler = new CreateSnapshotHandler(_mockSnapshotService.Object);
    }

    [Fact]
    public void Constructor_WithNullSnapshotService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new CreateSnapshotHandler(null!);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("snapshotService");
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateAndSaveSnapshot()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var periods = new List<PeriodInfo>
        {
            new PeriodInfo(2024, 1),
            new PeriodInfo(2024, 2)
        };
        var downloadPath = @"C:\Downloads";
        var command = new CreateSnapshotCommand(sessionId, periods, downloadPath);

        var snapshot = new DownloadSnapshot(sessionId, periods, downloadPath);
        
        _mockSnapshotService
            .Setup(x => x.CreateInitialSnapshotAsync(sessionId, periods, downloadPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);

        _mockSnapshotService
            .Setup(x => x.SaveSnapshotAsync(snapshot, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.SnapshotId.Should().Be(snapshot.Id);
        result.ErrorMessage.Should().BeNull();

        _mockSnapshotService.Verify(x => x.CreateInitialSnapshotAsync(sessionId, periods, downloadPath, It.IsAny<CancellationToken>()), Times.Once);
        _mockSnapshotService.Verify(x => x.SaveSnapshotAsync(snapshot, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCreateSnapshotThrows_ShouldReturnFailureResult()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var periods = new List<PeriodInfo> { new PeriodInfo(2024, 1) };
        var downloadPath = @"C:\Downloads";
        var command = new CreateSnapshotCommand(sessionId, periods, downloadPath);
        var errorMessage = "Failed to create snapshot";

        _mockSnapshotService
            .Setup(x => x.CreateInitialSnapshotAsync(sessionId, periods, downloadPath, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.SnapshotId.Should().Be(Guid.Empty);
        result.ErrorMessage.Should().Be(errorMessage);

        _mockSnapshotService.Verify(x => x.CreateInitialSnapshotAsync(sessionId, periods, downloadPath, It.IsAny<CancellationToken>()), Times.Once);
        _mockSnapshotService.Verify(x => x.SaveSnapshotAsync(It.IsAny<DownloadSnapshot>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSaveSnapshotThrows_ShouldReturnFailureResult()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var periods = new List<PeriodInfo> { new PeriodInfo(2024, 1) };
        var downloadPath = @"C:\Downloads";
        var command = new CreateSnapshotCommand(sessionId, periods, downloadPath);
        var errorMessage = "Failed to save snapshot";

        var snapshot = new DownloadSnapshot(sessionId, periods, downloadPath);
        
        _mockSnapshotService
            .Setup(x => x.CreateInitialSnapshotAsync(sessionId, periods, downloadPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);

        _mockSnapshotService
            .Setup(x => x.SaveSnapshotAsync(snapshot, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.SnapshotId.Should().Be(Guid.Empty);
        result.ErrorMessage.Should().Be(errorMessage);

        _mockSnapshotService.Verify(x => x.CreateInitialSnapshotAsync(sessionId, periods, downloadPath, It.IsAny<CancellationToken>()), Times.Once);
        _mockSnapshotService.Verify(x => x.SaveSnapshotAsync(snapshot, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyPeriods_ShouldStillWork()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var periods = new List<PeriodInfo>();
        var downloadPath = @"C:\Downloads";
        var command = new CreateSnapshotCommand(sessionId, periods, downloadPath);

        var snapshot = new DownloadSnapshot(sessionId, periods, downloadPath);
        
        _mockSnapshotService
            .Setup(x => x.CreateInitialSnapshotAsync(sessionId, periods, downloadPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);

        _mockSnapshotService
            .Setup(x => x.SaveSnapshotAsync(snapshot, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.SnapshotId.Should().Be(snapshot.Id);
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassTokenToServices()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var periods = new List<PeriodInfo> { new PeriodInfo(2024, 1) };
        var downloadPath = @"C:\Downloads";
        var command = new CreateSnapshotCommand(sessionId, periods, downloadPath);
        var cancellationToken = new CancellationToken();

        var snapshot = new DownloadSnapshot(sessionId, periods, downloadPath);
        
        _mockSnapshotService
            .Setup(x => x.CreateInitialSnapshotAsync(sessionId, periods, downloadPath, cancellationToken))
            .ReturnsAsync(snapshot);

        _mockSnapshotService
            .Setup(x => x.SaveSnapshotAsync(snapshot, cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();

        _mockSnapshotService.Verify(x => x.CreateInitialSnapshotAsync(sessionId, periods, downloadPath, cancellationToken), Times.Once);
        _mockSnapshotService.Verify(x => x.SaveSnapshotAsync(snapshot, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Handle_WithMultiplePeriods_ShouldHandleCorrectly()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var periods = new List<PeriodInfo>
        {
            new PeriodInfo(2024, 1),
            new PeriodInfo(2024, 2),
            new PeriodInfo(2024, 3),
            new PeriodInfo(2023, 12)
        };
        var downloadPath = @"C:\Downloads";
        var command = new CreateSnapshotCommand(sessionId, periods, downloadPath);

        var snapshot = new DownloadSnapshot(sessionId, periods, downloadPath);
        
        _mockSnapshotService
            .Setup(x => x.CreateInitialSnapshotAsync(sessionId, periods, downloadPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);

        _mockSnapshotService
            .Setup(x => x.SaveSnapshotAsync(snapshot, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.SnapshotId.Should().Be(snapshot.Id);
        result.ErrorMessage.Should().BeNull();
    }
}
