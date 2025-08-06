using FluentAssertions;
using Moq;
using NominaDownloaderPEIGTO.Application.Handlers;
using NominaDownloaderPEIGTO.Application.Interfaces;
using NominaDownloaderPEIGTO.Application.Queries;
using NominaDownloaderPEIGTO.Domain.Exceptions;
using NominaDownloaderPEIGTO.Domain.ValueObjects;

namespace NominaDownloaderPEIGTO.Tests.Application.Handlers;

public class GetAvailablePeriodsHandlerTests
{
    private readonly Mock<IWebPortalService> _mockWebPortalService;
    private readonly GetAvailablePeriodsHandler _handler;

    public GetAvailablePeriodsHandlerTests()
    {
        _mockWebPortalService = new Mock<IWebPortalService>();
        _handler = new GetAvailablePeriodsHandler(_mockWebPortalService.Object);
    }

    [Fact]
    public void Constructor_WithNullWebPortalService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new GetAvailablePeriodsHandler(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("webPortalService");
    }

    [Fact]
    public async Task Handle_WithValidSessionAndCredentials_ShouldReturnPeriodsSuccessfully()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "password");
        var query = new GetAvailablePeriodsQuery(credentials, 2024);
        var expectedPeriods = new List<PeriodInfo>
        {
            new PeriodInfo(2024, 1),
            new PeriodInfo(2024, 2),
            new PeriodInfo(2024, 3)
        };

        _mockWebPortalService
            .Setup(x => x.ValidateSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockWebPortalService
            .Setup(x => x.GetAvailablePeriodsAsync(2024, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPeriods);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Periods.Should().BeEquivalentTo(expectedPeriods);
        result.ErrorMessage.Should().BeNull();

        _mockWebPortalService.Verify(
            x => x.ValidateSessionAsync(It.IsAny<CancellationToken>()), 
            Times.Once);

        _mockWebPortalService.Verify(
            x => x.GetAvailablePeriodsAsync(2024, It.IsAny<CancellationToken>()), 
            Times.Once);

        // No debería hacer login porque la sesión es válida
        _mockWebPortalService.Verify(
            x => x.LoginAsync(It.IsAny<LoginCredentials>(), It.IsAny<CancellationToken>()), 
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithInvalidSession_ShouldLoginAndReturnPeriodsSuccessfully()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "password");
        var query = new GetAvailablePeriodsQuery(credentials, 2024);
        var expectedPeriods = new List<PeriodInfo>
        {
            new PeriodInfo(2024, 1),
            new PeriodInfo(2024, 2)
        };

        _mockWebPortalService
            .Setup(x => x.ValidateSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockWebPortalService
            .Setup(x => x.LoginAsync(credentials, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockWebPortalService
            .Setup(x => x.GetAvailablePeriodsAsync(2024, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPeriods);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Periods.Should().BeEquivalentTo(expectedPeriods);

        _mockWebPortalService.Verify(
            x => x.ValidateSessionAsync(It.IsAny<CancellationToken>()), 
            Times.Once);

        _mockWebPortalService.Verify(
            x => x.LoginAsync(credentials, It.IsAny<CancellationToken>()), 
            Times.Once);

        _mockWebPortalService.Verify(
            x => x.GetAvailablePeriodsAsync(2024, It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenLoginFails_ShouldReturnFailureResult()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "wrongpassword");
        var query = new GetAvailablePeriodsQuery(credentials, 2024);

        _mockWebPortalService
            .Setup(x => x.ValidateSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockWebPortalService
            .Setup(x => x.LoginAsync(credentials, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Periods.Should().BeEmpty();
        result.ErrorMessage.Should().Be("Falló el login al portal");

        _mockWebPortalService.Verify(
            x => x.ValidateSessionAsync(It.IsAny<CancellationToken>()), 
            Times.Once);

        _mockWebPortalService.Verify(
            x => x.LoginAsync(credentials, It.IsAny<CancellationToken>()), 
            Times.Once);

        // No debería intentar obtener períodos después del fallo de login
        _mockWebPortalService.Verify(
            x => x.GetAvailablePeriodsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), 
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenWebPortalServiceThrows_ShouldReturnFailureResult()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "password");
        var query = new GetAvailablePeriodsQuery(credentials, 2024);

        _mockWebPortalService
            .Setup(x => x.ValidateSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockWebPortalService
            .Setup(x => x.GetAvailablePeriodsAsync(2024, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Error del portal"));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Periods.Should().BeEmpty();
        result.ErrorMessage.Should().Be("Error del portal");

        _mockWebPortalService.Verify(
            x => x.GetAvailablePeriodsAsync(2024, It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyPeriodsResult_ShouldReturnSuccessWithEmptyList()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "password");
        var query = new GetAvailablePeriodsQuery(credentials, 2024);
        var emptyPeriods = new List<PeriodInfo>();

        _mockWebPortalService
            .Setup(x => x.ValidateSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockWebPortalService
            .Setup(x => x.GetAvailablePeriodsAsync(2024, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyPeriods);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Periods.Should().BeEmpty();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassTokenToAllServices()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "password");
        var query = new GetAvailablePeriodsQuery(credentials, 2024);
        var cancellationToken = new CancellationTokenSource().Token;
        var expectedPeriods = new List<PeriodInfo> { new PeriodInfo(2024, 1) };

        _mockWebPortalService
            .Setup(x => x.ValidateSessionAsync(cancellationToken))
            .ReturnsAsync(false);

        _mockWebPortalService
            .Setup(x => x.LoginAsync(credentials, cancellationToken))
            .ReturnsAsync(true);

        _mockWebPortalService
            .Setup(x => x.GetAvailablePeriodsAsync(2024, cancellationToken))
            .ReturnsAsync(expectedPeriods);

        // Act
        await _handler.Handle(query, cancellationToken);

        // Assert
        _mockWebPortalService.Verify(
            x => x.ValidateSessionAsync(cancellationToken), 
            Times.Once);

        _mockWebPortalService.Verify(
            x => x.LoginAsync(credentials, cancellationToken), 
            Times.Once);

        _mockWebPortalService.Verify(
            x => x.GetAvailablePeriodsAsync(2024, cancellationToken), 
            Times.Once);
    }
}
