using FluentAssertions;
using Moq;
using NominaDownloaderPEIGTO.Application.Commands;
using NominaDownloaderPEIGTO.Application.Handlers;
using NominaDownloaderPEIGTO.Application.Interfaces;
using NominaDownloaderPEIGTO.Domain.Entities;
using NominaDownloaderPEIGTO.Domain.ValueObjects;

namespace NominaDownloaderPEIGTO.Tests.Application.Handlers;

public class StartDownloadSessionHandlerTests
{
    private readonly Mock<IDownloadSessionRepository> _mockSessionRepository;
    private readonly Mock<IWebPortalService> _mockWebPortalService;
    private readonly Mock<IProgressService> _mockProgressService;
    private readonly Mock<IParallelDownloadService> _mockDownloadService;
    private readonly StartDownloadSessionHandler _handler;

    public StartDownloadSessionHandlerTests()
    {
        _mockSessionRepository = new Mock<IDownloadSessionRepository>();
        _mockWebPortalService = new Mock<IWebPortalService>();
        _mockProgressService = new Mock<IProgressService>();
        _mockDownloadService = new Mock<IParallelDownloadService>();
        
        _handler = new StartDownloadSessionHandler(
            _mockSessionRepository.Object,
            _mockWebPortalService.Object,
            _mockProgressService.Object,
            _mockDownloadService.Object);
    }

    [Fact]
    public void Constructor_WithNullSessionRepository_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new StartDownloadSessionHandler(
            null!,
            _mockWebPortalService.Object,
            _mockProgressService.Object,
            _mockDownloadService.Object);
            
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("sessionRepository");
    }

    [Fact]
    public void Constructor_WithNullWebPortalService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new StartDownloadSessionHandler(
            _mockSessionRepository.Object,
            null!,
            _mockProgressService.Object,
            _mockDownloadService.Object);
            
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("webPortalService");
    }

    [Fact]
    public void Constructor_WithNullProgressService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new StartDownloadSessionHandler(
            _mockSessionRepository.Object,
            _mockWebPortalService.Object,
            null!,
            _mockDownloadService.Object);
            
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("progressService");
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateAndStartSession()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "pass");
        var config = new DownloadConfig(@"C:\Downloads");
        var periods = new[] { new PeriodInfo(2024, 1), new PeriodInfo(2024, 2) };
        
        var command = new StartDownloadSessionCommand(credentials, config, periods);

        _mockSessionRepository
            .Setup(x => x.CreateAsync(It.IsAny<DownloadSession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(It.IsAny<DownloadSession>());

        _mockSessionRepository
            .Setup(x => x.UpdateAsync(It.IsAny<DownloadSession>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockProgressService
            .Setup(x => x.NotifySessionStartedAsync(It.IsAny<DownloadSession>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.SessionId.Should().NotBeEmpty();

        _mockSessionRepository.Verify(
            x => x.CreateAsync(It.IsAny<DownloadSession>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _mockSessionRepository.Verify(
            x => x.UpdateAsync(It.IsAny<DownloadSession>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _mockProgressService.Verify(
            x => x.NotifySessionStartedAsync(It.IsAny<DownloadSession>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrows_ShouldReturnFailureResult()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "pass");
        var config = new DownloadConfig(@"C:\Downloads");
        var periods = new[] { new PeriodInfo(2024, 1) };
        
        var command = new StartDownloadSessionCommand(credentials, config, periods);

        _mockSessionRepository
            .Setup(x => x.CreateAsync(It.IsAny<DownloadSession>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Database error");
    }
}
