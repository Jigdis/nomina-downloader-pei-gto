using FluentAssertions;
using NominaDownloaderPEIGTO.Domain.Entities;
using NominaDownloaderPEIGTO.Domain.ValueObjects;

namespace NominaDownloaderPEIGTO.Tests.Domain.Entities;

public class FailedTaskTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);
        var errorMessage = "Download failed";
        var attemptNumber = 1;
        var stackTrace = "Stack trace details";
        var browserInfo = "Chrome 120";

        // Act
        var failedTask = new FailedTask(period, errorMessage, attemptNumber, stackTrace, browserInfo);

        // Assert
        failedTask.Id.Should().NotBeEmpty();
        failedTask.Period.Should().Be(period);
        failedTask.ErrorMessage.Should().Be(errorMessage);
        failedTask.AttemptNumber.Should().Be(attemptNumber);
        failedTask.StackTrace.Should().Be(stackTrace);
        failedTask.BrowserInfo.Should().Be(browserInfo);
        failedTask.FailedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_WithMinimalParameters_ShouldCreateInstance()
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);
        var errorMessage = "Download failed";
        var attemptNumber = 1;

        // Act
        var failedTask = new FailedTask(period, errorMessage, attemptNumber);

        // Assert
        failedTask.Id.Should().NotBeEmpty();
        failedTask.Period.Should().Be(period);
        failedTask.ErrorMessage.Should().Be(errorMessage);
        failedTask.AttemptNumber.Should().Be(attemptNumber);
        failedTask.StackTrace.Should().BeNull();
        failedTask.BrowserInfo.Should().BeNull();
        failedTask.FailedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_WithNullPeriod_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new FailedTask(null!, "Error message", 1);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("period");
    }

    [Fact]
    public void Constructor_WithNullErrorMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);

        // Act & Assert
        var act = () => new FailedTask(period, null!, 1);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("errorMessage");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(10)]
    public void Constructor_WithDifferentAttemptNumbers_ShouldSetCorrectly(int attemptNumber)
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);
        var errorMessage = "Download failed";

        // Act
        var failedTask = new FailedTask(period, errorMessage, attemptNumber);

        // Assert
        failedTask.AttemptNumber.Should().Be(attemptNumber);
    }

    [Fact]
    public void DisplayMessage_ShouldCombinePeriodAndError()
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);
        var errorMessage = "Connection timeout";
        var attemptNumber = 2;
        var failedTask = new FailedTask(period, errorMessage, attemptNumber);

        // Act
        var displayMessage = failedTask.DisplayMessage;

        // Assert
        displayMessage.Should().Contain(period.DisplayName);
        displayMessage.Should().Contain("Connection timeout");
        displayMessage.Should().Contain("Intento 2");
        displayMessage.Should().Match("Fallo en * (Intento 2): Connection timeout");
    }

    [Fact]
    public void ToString_ShouldReturnDisplayMessage()
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);
        var errorMessage = "Network error";
        var attemptNumber = 1;
        var failedTask = new FailedTask(period, errorMessage, attemptNumber);

        // Act
        var result = failedTask.ToString();

        // Assert
        result.Should().Be(failedTask.DisplayMessage);
    }

    [Fact]
    public void Id_ShouldBeUnique()
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);
        var errorMessage = "Error";

        // Act
        var task1 = new FailedTask(period, errorMessage, 1);
        var task2 = new FailedTask(period, errorMessage, 1);

        // Assert
        task1.Id.Should().NotBe(task2.Id);
    }

    [Fact]
    public void Constructor_WithEmptyErrorMessage_ShouldNotThrow()
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);
        var errorMessage = "";
        var attemptNumber = 1;

        // Act
        var act = () => new FailedTask(period, errorMessage, attemptNumber);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithLongStackTrace_ShouldStoreCorrectly()
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);
        var errorMessage = "Error";
        var attemptNumber = 1;
        var longStackTrace = new string('x', 1000); // Very long stack trace

        // Act
        var failedTask = new FailedTask(period, errorMessage, attemptNumber, longStackTrace);

        // Assert
        failedTask.StackTrace.Should().Be(longStackTrace);
        failedTask.StackTrace?.Length.Should().Be(1000);
    }

    [Fact]
    public void DisplayMessage_WithDifferentPeriods_ShouldShowCorrectFormat()
    {
        // Arrange
        var period1 = new PeriodInfo(2024, 1);
        var period2 = new PeriodInfo(2023, 12);
        var errorMessage = "Test error";

        // Act
        var task1 = new FailedTask(period1, errorMessage, 1);
        var task2 = new FailedTask(period2, errorMessage, 2);

        // Assert
        task1.DisplayMessage.Should().Contain("Período 01: Enero");
        task1.DisplayMessage.Should().Contain("Intento 1");
        task2.DisplayMessage.Should().Contain("Período 12: Diciembre");
        task2.DisplayMessage.Should().Contain("Intento 2");
    }
}
