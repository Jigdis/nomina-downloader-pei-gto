using FluentAssertions;
using Moq;
using NominaDownloaderPEIGTO.Application.Interfaces;
using NominaDownloaderPEIGTO.Domain.ValueObjects;
using NominaDownloaderPEIGTO.Domain.Enums;

namespace NominaDownloaderPEIGTO.Tests.Infrastructure.Services;

/// <summary>
/// Tests unitarios para IWebPortalService usando mocks
/// Estos tests reemplazan a los tests de integración de SeleniumWebPortalService
/// </summary>
public class WebPortalServiceMockTests
{
    private readonly Mock<IWebPortalService> _mockWebPortalService;
    private readonly DownloadConfig _validConfig;

    public WebPortalServiceMockTests()
    {
        _mockWebPortalService = new Mock<IWebPortalService>();
        _validConfig = new DownloadConfig(@"C:\Downloads");
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnTrue()
    {
        // Arrange
        var credentials = new LoginCredentials("validuser", "validpass");
        _mockWebPortalService
            .Setup(x => x.LoginAsync(credentials, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _mockWebPortalService.Object.LoginAsync(credentials);

        // Assert
        result.Should().BeTrue();
        _mockWebPortalService.Verify(x => x.LoginAsync(credentials, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidCredentials_ShouldReturnFalse()
    {
        // Arrange
        var invalidCredentials = new LoginCredentials("invalid", "invalid");
        _mockWebPortalService
            .Setup(x => x.LoginAsync(invalidCredentials, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _mockWebPortalService.Object.LoginAsync(invalidCredentials);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetAvailableYearsAsync_WhenLoggedIn_ShouldReturnYears()
    {
        // Arrange
        var expectedYears = new[] { 2022, 2023, 2024 };
        _mockWebPortalService
            .Setup(x => x.GetAvailableYearsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedYears);

        // Act
        var result = await _mockWebPortalService.Object.GetAvailableYearsAsync();

        // Assert
        result.Should().BeEquivalentTo(expectedYears);
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAvailablePeriodsAsync_WithValidYear_ShouldReturnPeriods()
    {
        // Arrange
        var year = 2024;
        var expectedPeriods = new[]
        {
            new PeriodInfo(2024, 1),
            new PeriodInfo(2024, 2),
            new PeriodInfo(2024, 3)
        };

        _mockWebPortalService
            .Setup(x => x.GetAvailablePeriodsAsync(year, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPeriods);

        // Act
        var result = await _mockWebPortalService.Object.GetAvailablePeriodsAsync(year);

        // Assert
        result.Should().BeEquivalentTo(expectedPeriods);
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task DownloadFileAsync_WithValidPeriod_ShouldReturnFileMetadata()
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);
        var downloadPath = @"C:\Downloads";
        var expectedMetadata = new FileMetadata(
            "2024_01_recibo.pdf", 
            @"C:\Downloads\2024_01_recibo.pdf", 
            1024, 
            FileType.ReciboPdf, 
            DateTime.UtcNow, 
            "hash123");

        _mockWebPortalService
            .Setup(x => x.DownloadFileAsync(period, downloadPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedMetadata);

        // Act
        var result = await _mockWebPortalService.Object.DownloadFileAsync(period, downloadPath);

        // Assert
        result.Should().Be(expectedMetadata);
        result.FileName.Should().Be("2024_01_recibo.pdf");
    }

    [Fact]
    public async Task LogoutAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        _mockWebPortalService
            .Setup(x => x.LogoutAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var act = async () => await _mockWebPortalService.Object.LogoutAsync();

        // Assert
        await act.Should().NotThrowAsync();
        _mockWebPortalService.Verify(x => x.LogoutAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ValidateSessionAsync_WhenSessionValid_ShouldReturnTrue()
    {
        // Arrange
        _mockWebPortalService
            .Setup(x => x.ValidateSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _mockWebPortalService.Object.ValidateSessionAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(2023)]
    [InlineData(2024)]
    [InlineData(2025)]
    public async Task GetAvailablePeriodsAsync_WithDifferentYears_ShouldHandleCorrectly(int year)
    {
        // Arrange
        var expectedPeriods = new[]
        {
            new PeriodInfo(year, 1),
            new PeriodInfo(year, 2)
        };

        _mockWebPortalService
            .Setup(x => x.GetAvailablePeriodsAsync(year, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPeriods);

        // Act
        var result = await _mockWebPortalService.Object.GetAvailablePeriodsAsync(year);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.Year == year);
    }

    [Fact]
    public async Task ServiceOperations_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var credentials = new LoginCredentials("user", "pass");
        
        _mockWebPortalService
            .Setup(x => x.LoginAsync(credentials, cancellationTokenSource.Token))
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var act = async () => await _mockWebPortalService.Object.LoginAsync(credentials, cancellationTokenSource.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void ValueObjects_ShouldWorkCorrectlyWithService()
    {
        // Arrange & Act
        var credentials = new LoginCredentials("testuser", "testpass");
        var period = new PeriodInfo(2024, 6);
        var config = new DownloadConfig(@"C:\TestDownloads");

        // Assert
        credentials.Username.Should().Be("testuser");
        credentials.Password.Should().Be("testpass");
        period.Year.Should().Be(2024);
        period.Period.Should().Be(6);
        period.DisplayName.Should().Be("Período 06: Junio");
        config.DownloadPath.Should().Be(@"C:\TestDownloads");
    }
}
