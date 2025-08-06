using FluentAssertions;
using Moq;
using NominaDownloaderPEIGTO.Application.Handlers;
using NominaDownloaderPEIGTO.Application.Interfaces;
using NominaDownloaderPEIGTO.Application.Queries;
using NominaDownloaderPEIGTO.Domain.Exceptions;
using NominaDownloaderPEIGTO.Domain.ValueObjects;

namespace NominaDownloaderPEIGTO.Tests.Application.Handlers;

public class GetAvailableYearsHandlerTests
{
    private readonly Mock<IWebPortalService> _mockWebPortalService;
    private readonly GetAvailableYearsHandler _handler;

    public GetAvailableYearsHandlerTests()
    {
        _mockWebPortalService = new Mock<IWebPortalService>();
        _handler = new GetAvailableYearsHandler(_mockWebPortalService.Object);
    }

    [Fact]
    public void Constructor_WithNullWebPortalService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new GetAvailableYearsHandler(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ShouldReturnYearsSuccessfully()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "password");
        var query = new GetAvailableYearsQuery(credentials);
        var expectedYears = new List<int> { 2022, 2023, 2024 };

        _mockWebPortalService
            .Setup(x => x.LoginAsync(credentials, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockWebPortalService
            .Setup(x => x.GetAvailableYearsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedYears);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Years.Should().BeEquivalentTo(expectedYears);
        result.ErrorMessage.Should().BeNull();

        _mockWebPortalService.Verify(
            x => x.LoginAsync(credentials, It.IsAny<CancellationToken>()), 
            Times.Once);

        _mockWebPortalService.Verify(
            x => x.GetAvailableYearsAsync(It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenLoginFails_ShouldReturnFailureResult()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "wrongpassword");
        var query = new GetAvailableYearsQuery(credentials);

        _mockWebPortalService
            .Setup(x => x.LoginAsync(credentials, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Years.Should().BeEmpty();
        result.ErrorMessage.Should().Be("Error al iniciar sesión en el portal");

        _mockWebPortalService.Verify(
            x => x.LoginAsync(credentials, It.IsAny<CancellationToken>()), 
            Times.Once);

        // No debería intentar obtener años después del fallo de login
        _mockWebPortalService.Verify(
            x => x.GetAvailableYearsAsync(It.IsAny<CancellationToken>()), 
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenWebPortalServiceThrows_ShouldReturnFailureResult()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "password");
        var query = new GetAvailableYearsQuery(credentials);

        _mockWebPortalService
            .Setup(x => x.LoginAsync(credentials, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockWebPortalService
            .Setup(x => x.GetAvailableYearsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Error del portal"));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Years.Should().BeEmpty();
        result.ErrorMessage.Should().Be("Error obteniendo años disponibles: Error del portal");

        _mockWebPortalService.Verify(
            x => x.LoginAsync(credentials, It.IsAny<CancellationToken>()), 
            Times.Once);

        _mockWebPortalService.Verify(
            x => x.GetAvailableYearsAsync(It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyYearsResult_ShouldReturnSuccessWithEmptyList()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "password");
        var query = new GetAvailableYearsQuery(credentials);
        var emptyYears = new List<int>();

        _mockWebPortalService
            .Setup(x => x.LoginAsync(credentials, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockWebPortalService
            .Setup(x => x.GetAvailableYearsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyYears);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Years.Should().BeEmpty();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithOrderedYears_ShouldReturnYearsInOrder()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "password");
        var query = new GetAvailableYearsQuery(credentials);
        var unorderedYears = new List<int> { 2024, 2022, 2023, 2021 };

        _mockWebPortalService
            .Setup(x => x.LoginAsync(credentials, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockWebPortalService
            .Setup(x => x.GetAvailableYearsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(unorderedYears);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Years.Should().BeEquivalentTo(unorderedYears);
    }

    [Fact]
    public async Task Handle_WithNetworkException_ShouldReturnFailureResult()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "password");
        var query = new GetAvailableYearsQuery(credentials);

        _mockWebPortalService
            .Setup(x => x.LoginAsync(credentials, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Years.Should().BeEmpty();
        result.ErrorMessage.Should().Be("Error obteniendo años disponibles: Network error");
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassTokenToAllServices()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "password");
        var query = new GetAvailableYearsQuery(credentials);
        var cancellationToken = new CancellationTokenSource().Token;
        var expectedYears = new List<int> { 2023, 2024 };

        _mockWebPortalService
            .Setup(x => x.LoginAsync(credentials, cancellationToken))
            .ReturnsAsync(true);

        _mockWebPortalService
            .Setup(x => x.GetAvailableYearsAsync(cancellationToken))
            .ReturnsAsync(expectedYears);

        // Act
        await _handler.Handle(query, cancellationToken);

        // Assert
        _mockWebPortalService.Verify(
            x => x.LoginAsync(credentials, cancellationToken), 
            Times.Once);

        _mockWebPortalService.Verify(
            x => x.GetAvailableYearsAsync(cancellationToken), 
            Times.Once);
    }
}
