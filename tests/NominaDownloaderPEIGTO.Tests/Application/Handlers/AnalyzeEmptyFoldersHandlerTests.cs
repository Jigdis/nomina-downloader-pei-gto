using FluentAssertions;
using Moq;
using NominaDownloaderPEIGTO.Application.Handlers;
using NominaDownloaderPEIGTO.Application.Interfaces;
using NominaDownloaderPEIGTO.Application.Queries;
using NominaDownloaderPEIGTO.Domain.Entities;
using NominaDownloaderPEIGTO.Domain.ValueObjects;

namespace NominaDownloaderPEIGTO.Tests.Application.Handlers;

public class AnalyzeEmptyFoldersHandlerTests
{
    private readonly Mock<IDownloadSnapshotService> _mockSnapshotService;
    private readonly AnalyzeEmptyFoldersHandler _handler;

    public AnalyzeEmptyFoldersHandlerTests()
    {
        _mockSnapshotService = new Mock<IDownloadSnapshotService>();
        _handler = new AnalyzeEmptyFoldersHandler(_mockSnapshotService.Object);
    }

    [Fact]
    public void Constructor_WithNullSnapshotService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new AnalyzeEmptyFoldersHandler(null!);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("snapshotService");
    }

    [Fact]
    public async Task Handle_WithValidSessionAndEmptyFolders_ShouldReturnAnalysisResult()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var query = new AnalyzeEmptyFoldersQuery(sessionId);

        var failedPeriods = new List<PeriodInfo>
        {
            new PeriodInfo(2024, 1),
            new PeriodInfo(2024, 2)
        };

        var emptyFolders = new List<string>
        {
            @"C:\Downloads\2024\Enero",
            @"C:\Downloads\2024\Febrero"
        };

        var snapshot = new DownloadSnapshot(sessionId, failedPeriods, @"C:\Downloads");
        
        _mockSnapshotService
            .Setup(x => x.AnalyzeEmptyFoldersAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedPeriods);

        _mockSnapshotService
            .Setup(x => x.GetSnapshotBySessionIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);

        // Act
        var result = await _handler.Handle(query);

        // Assert
        result.Should().NotBeNull();
        result.SessionId.Should().Be(sessionId);
        result.FailedPeriods.Should().BeEquivalentTo(failedPeriods);
        result.HasEmptyFolders.Should().BeTrue();
        result.EmptyFolders.Should().NotBeNull();

        _mockSnapshotService.Verify(x => x.AnalyzeEmptyFoldersAsync(sessionId, It.IsAny<CancellationToken>()), Times.Once);
        _mockSnapshotService.Verify(x => x.GetSnapshotBySessionIdAsync(sessionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNoEmptyFolders_ShouldReturnResultWithoutEmptyFolders()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var query = new AnalyzeEmptyFoldersQuery(sessionId);

        var failedPeriods = new List<PeriodInfo>();
        var snapshot = new DownloadSnapshot(sessionId, new List<PeriodInfo>(), @"C:\Downloads");
        
        _mockSnapshotService
            .Setup(x => x.AnalyzeEmptyFoldersAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedPeriods);

        _mockSnapshotService
            .Setup(x => x.GetSnapshotBySessionIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);

        // Act
        var result = await _handler.Handle(query);

        // Assert
        result.Should().NotBeNull();
        result.SessionId.Should().Be(sessionId);
        result.FailedPeriods.Should().BeEmpty();
        result.HasEmptyFolders.Should().BeFalse();
        result.EmptyFolders.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WhenSnapshotNotFound_ShouldReturnResultWithEmptyFoldersFromService()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var query = new AnalyzeEmptyFoldersQuery(sessionId);

        var failedPeriods = new List<PeriodInfo>
        {
            new PeriodInfo(2024, 1)
        };

        _mockSnapshotService
            .Setup(x => x.AnalyzeEmptyFoldersAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedPeriods);

        _mockSnapshotService
            .Setup(x => x.GetSnapshotBySessionIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DownloadSnapshot?)null);

        // Act
        var result = await _handler.Handle(query);

        // Assert
        result.Should().NotBeNull();
        result.SessionId.Should().Be(sessionId);
        result.FailedPeriods.Should().BeEquivalentTo(failedPeriods);
        result.HasEmptyFolders.Should().BeTrue();
        result.EmptyFolders.Should().BeEmpty(); // Should be empty list when snapshot is null
    }

    [Fact]
    public async Task Handle_WhenServiceThrowsException_ShouldReturnEmptyResult()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var query = new AnalyzeEmptyFoldersQuery(sessionId);

        _mockSnapshotService
            .Setup(x => x.AnalyzeEmptyFoldersAsync(sessionId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _handler.Handle(query);

        // Assert
        result.Should().NotBeNull();
        result.SessionId.Should().Be(sessionId);
        result.FailedPeriods.Should().BeEmpty();
        result.HasEmptyFolders.Should().BeFalse();
        result.EmptyFolders.Should().BeEmpty();

        _mockSnapshotService.Verify(x => x.AnalyzeEmptyFoldersAsync(sessionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassTokenToServices()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var query = new AnalyzeEmptyFoldersQuery(sessionId);
        var cancellationToken = new CancellationToken();

        var failedPeriods = new List<PeriodInfo>();
        var snapshot = new DownloadSnapshot(sessionId, failedPeriods, @"C:\Downloads");
        
        _mockSnapshotService
            .Setup(x => x.AnalyzeEmptyFoldersAsync(sessionId, cancellationToken))
            .ReturnsAsync(failedPeriods);

        _mockSnapshotService
            .Setup(x => x.GetSnapshotBySessionIdAsync(sessionId, cancellationToken))
            .ReturnsAsync(snapshot);

        // Act
        var result = await _handler.Handle(query, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.SessionId.Should().Be(sessionId);

        _mockSnapshotService.Verify(x => x.AnalyzeEmptyFoldersAsync(sessionId, cancellationToken), Times.Once);
        _mockSnapshotService.Verify(x => x.GetSnapshotBySessionIdAsync(sessionId, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Handle_WithMixedResults_ShouldReturnCorrectAnalysis()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var query = new AnalyzeEmptyFoldersQuery(sessionId);

        var failedPeriods = new List<PeriodInfo>
        {
            new PeriodInfo(2024, 1),
            new PeriodInfo(2024, 3)
        };

        var allPeriods = new List<PeriodInfo>
        {
            new PeriodInfo(2024, 1),
            new PeriodInfo(2024, 2),
            new PeriodInfo(2024, 3)
        };

        var snapshot = new DownloadSnapshot(sessionId, allPeriods, @"C:\Downloads");
        
        _mockSnapshotService
            .Setup(x => x.AnalyzeEmptyFoldersAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedPeriods);

        _mockSnapshotService
            .Setup(x => x.GetSnapshotBySessionIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);

        // Act
        var result = await _handler.Handle(query);

        // Assert
        result.Should().NotBeNull();
        result.SessionId.Should().Be(sessionId);
        result.FailedPeriods.Should().HaveCount(2);
        result.FailedPeriods.Should().BeEquivalentTo(failedPeriods);
        result.HasEmptyFolders.Should().BeTrue();
    }
}
