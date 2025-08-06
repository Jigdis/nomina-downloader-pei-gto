using FluentAssertions;
using NominaDownloaderPEIGTO.Domain.Entities;
using NominaDownloaderPEIGTO.Domain.ValueObjects;
using NominaDownloaderPEIGTO.Domain.Enums;

namespace NominaDownloaderPEIGTO.Tests.Domain.Entities;

public class ErrorRecoverySessionTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange
        var originalSessionId = Guid.NewGuid();
        var maxRetryAttempts = 5;

        // Act
        var session = new ErrorRecoverySession(originalSessionId, maxRetryAttempts);

        // Assert
        session.Id.Should().NotBeEmpty();
        session.OriginalSessionId.Should().Be(originalSessionId);
        session.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        session.FailedAttempts.Should().BeEmpty();
        session.RecoveryAttempts.Should().BeEmpty();
        session.Status.Should().Be(ErrorRecoveryStatus.Pending);
        session.MaxRetryAttempts.Should().Be(maxRetryAttempts);
    }

    [Fact]
    public void Constructor_WithDefaultMaxRetryAttempts_ShouldSetToThree()
    {
        // Arrange
        var originalSessionId = Guid.NewGuid();

        // Act
        var session = new ErrorRecoverySession(originalSessionId);

        // Assert
        session.MaxRetryAttempts.Should().Be(3);
    }

    [Fact]
    public void AddFailedAttempt_ShouldAddToFailedAttemptsList()
    {
        // Arrange
        var session = new ErrorRecoverySession(Guid.NewGuid());
        var period = new PeriodInfo(2024, 1);
        var errorMessage = "Download failed";
        var folderPath = @"C:\Downloads\2024\01";

        // Act
        session.AddFailedAttempt(period, errorMessage, folderPath);

        // Assert
        session.FailedAttempts.Should().HaveCount(1);
        var attempt = session.FailedAttempts.First();
        attempt.Period.Should().Be(period);
        attempt.ErrorMessage.Should().Be(errorMessage);
        attempt.FolderPath.Should().Be(folderPath);
    }

    [Fact]
    public void StartRecovery_WithPendingStatus_ShouldChangeStatusToInProgress()
    {
        // Arrange
        var session = new ErrorRecoverySession(Guid.NewGuid());

        // Act
        session.StartRecovery();

        // Assert
        session.Status.Should().Be(ErrorRecoveryStatus.InProgress);
    }

    [Fact]
    public void StartRecovery_WithNonPendingStatus_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var session = new ErrorRecoverySession(Guid.NewGuid());
        session.StartRecovery(); // Set to InProgress

        // Act & Assert
        var act = () => session.StartRecovery();
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("La sesión de recuperación ya fue iniciada");
    }

    [Fact]
    public void CompleteRecovery_ShouldSetStatusToCompleted()
    {
        // Arrange
        var session = new ErrorRecoverySession(Guid.NewGuid());

        // Act
        session.CompleteRecovery();

        // Assert
        session.Status.Should().Be(ErrorRecoveryStatus.Completed);
    }

    [Fact]
    public void FailRecovery_ShouldSetStatusToFailed()
    {
        // Arrange
        var session = new ErrorRecoverySession(Guid.NewGuid());
        var errorMessage = "Recovery failed";

        // Act
        session.FailRecovery(errorMessage);

        // Assert
        session.Status.Should().Be(ErrorRecoveryStatus.Failed);
    }

    [Fact]
    public void GetPeriodsToRetry_WithNoRecoveryAttempts_ShouldReturnAllFailedPeriods()
    {
        // Arrange
        var session = new ErrorRecoverySession(Guid.NewGuid(), 3);
        var period1 = new PeriodInfo(2024, 1);
        var period2 = new PeriodInfo(2024, 2);
        
        session.AddFailedAttempt(period1, "Error 1", @"C:\Downloads\2024\01");
        session.AddFailedAttempt(period2, "Error 2", @"C:\Downloads\2024\02");

        // Act
        var periodsToRetry = session.GetPeriodsToRetry();

        // Assert
        periodsToRetry.Should().HaveCount(2);
        periodsToRetry.Should().Contain(period1);
        periodsToRetry.Should().Contain(period2);
    }

    [Fact]
    public void GetPeriodsToRetry_WithMaxRetryAttemptsReached_ShouldExcludeThosePeriods()
    {
        // Arrange
        var session = new ErrorRecoverySession(Guid.NewGuid(), 2);
        var period1 = new PeriodInfo(2024, 1);
        var period2 = new PeriodInfo(2024, 2);
        
        session.AddFailedAttempt(period1, "Error 1", @"C:\Downloads\2024\01");
        session.AddFailedAttempt(period2, "Error 2", @"C:\Downloads\2024\02");

        // Add max retry attempts for period1
        session.AddRecoveryAttempt(period1, false, "Retry 1 failed");
        session.AddRecoveryAttempt(period1, false, "Retry 2 failed");

        // Act
        var periodsToRetry = session.GetPeriodsToRetry();

        // Assert
        periodsToRetry.Should().HaveCount(1);
        periodsToRetry.Should().Contain(period2);
        periodsToRetry.Should().NotContain(period1);
    }

    [Fact]
    public void AddRecoveryAttempt_ShouldAddToRecoveryAttemptsList()
    {
        // Arrange
        var session = new ErrorRecoverySession(Guid.NewGuid());
        var period = new PeriodInfo(2024, 1);

        // Act
        session.AddRecoveryAttempt(period, true);

        // Assert
        session.RecoveryAttempts.Should().HaveCount(1);
        var attempt = session.RecoveryAttempts.First();
        attempt.Period.Should().Be(period);
        attempt.Success.Should().BeTrue();
        attempt.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void AddRecoveryAttempt_WithErrorMessage_ShouldStoreErrorMessage()
    {
        // Arrange
        var session = new ErrorRecoverySession(Guid.NewGuid());
        var period = new PeriodInfo(2024, 1);
        var errorMessage = "Retry failed";

        // Act
        session.AddRecoveryAttempt(period, false, errorMessage);

        // Assert
        var attempt = session.RecoveryAttempts.First();
        attempt.Success.Should().BeFalse();
        attempt.ErrorMessage.Should().Be(errorMessage);
    }

    [Fact]
    public void ShouldRetryPeriod_WithLessAttemptsThantMax_ShouldReturnTrue()
    {
        // Arrange
        var session = new ErrorRecoverySession(Guid.NewGuid(), 3);
        var period = new PeriodInfo(2024, 1);
        
        session.AddRecoveryAttempt(period, false, "First attempt failed");

        // Act
        var shouldRetry = session.ShouldRetryPeriod(period);

        // Assert
        shouldRetry.Should().BeTrue();
    }

    [Fact]
    public void ShouldRetryPeriod_WithMaxAttemptsReached_ShouldReturnFalse()
    {
        // Arrange
        var session = new ErrorRecoverySession(Guid.NewGuid(), 2);
        var period = new PeriodInfo(2024, 1);
        
        session.AddRecoveryAttempt(period, false, "First attempt failed");
        session.AddRecoveryAttempt(period, false, "Second attempt failed");

        // Act
        var shouldRetry = session.ShouldRetryPeriod(period);

        // Assert
        shouldRetry.Should().BeFalse();
    }

    [Fact]
    public void ShouldRetryPeriod_WithNoPreviousAttempts_ShouldReturnTrue()
    {
        // Arrange
        var session = new ErrorRecoverySession(Guid.NewGuid(), 3);
        var period = new PeriodInfo(2024, 1);

        // Act
        var shouldRetry = session.ShouldRetryPeriod(period);

        // Assert
        shouldRetry.Should().BeTrue();
    }

    [Fact]
    public void CleanupFailedFolder_WithNonExistingFolder_ShouldNotThrow()
    {
        // Arrange
        var session = new ErrorRecoverySession(Guid.NewGuid());
        var folderPath = @"C:\NonExistingFolder";

        // Act & Assert
        var act = () => session.CleanupFailedFolder(folderPath);
        act.Should().NotThrow();
    }

    [Fact]
    public void Id_ShouldBeUnique()
    {
        // Arrange
        var originalSessionId = Guid.NewGuid();

        // Act
        var session1 = new ErrorRecoverySession(originalSessionId);
        var session2 = new ErrorRecoverySession(originalSessionId);

        // Assert
        session1.Id.Should().NotBe(session2.Id);
    }
}

public class FailedDownloadAttemptTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);
        var errorMessage = "Download failed";
        var folderPath = @"C:\Downloads\2024\01";

        // Act
        var attempt = new FailedDownloadAttempt(period, errorMessage, folderPath);

        // Assert
        attempt.Period.Should().Be(period);
        attempt.ErrorMessage.Should().Be(errorMessage);
        attempt.FolderPath.Should().Be(folderPath);
        attempt.FailedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_WithNullPeriod_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new FailedDownloadAttempt(null!, "Error", @"C:\Path");
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("period");
    }

    [Fact]
    public void Constructor_WithNullErrorMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);

        // Act & Assert
        var act = () => new FailedDownloadAttempt(period, null!, @"C:\Path");
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("errorMessage");
    }

    [Fact]
    public void Constructor_WithNullFolderPath_ShouldThrowArgumentNullException()
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);

        // Act & Assert
        var act = () => new FailedDownloadAttempt(period, "Error", null!);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("folderPath");
    }
}

public class RecoveryAttemptTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);
        var success = true;
        var errorMessage = "No error";

        // Act
        var attempt = new RecoveryAttempt(period, success, errorMessage);

        // Assert
        attempt.Period.Should().Be(period);
        attempt.Success.Should().Be(success);
        attempt.ErrorMessage.Should().Be(errorMessage);
        attempt.AttemptedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_WithNullPeriod_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new RecoveryAttempt(null!, true);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("period");
    }

    [Fact]
    public void Constructor_WithoutErrorMessage_ShouldSetErrorMessageToNull()
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);

        // Act
        var attempt = new RecoveryAttempt(period, true);

        // Assert
        attempt.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithSuccessFalse_ShouldStoreSuccessCorrectly()
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);

        // Act
        var attempt = new RecoveryAttempt(period, false, "Failed");

        // Assert
        attempt.Success.Should().BeFalse();
        attempt.ErrorMessage.Should().Be("Failed");
    }
}
