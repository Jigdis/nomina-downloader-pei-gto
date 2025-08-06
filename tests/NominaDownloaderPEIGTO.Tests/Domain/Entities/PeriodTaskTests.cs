using FluentAssertions;
using NominaDownloaderPEIGTO.Domain.Entities;
using NominaDownloaderPEIGTO.Domain.Enums;
using NominaDownloaderPEIGTO.Domain.ValueObjects;

namespace NominaDownloaderPEIGTO.Tests.Domain.Entities;

public class PeriodTaskTests
{
    [Fact]
    public void Constructor_WithValidPeriod_ShouldCreateInstance()
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);

        // Act
        var task = new PeriodTask(period);

        // Assert
        task.Id.Should().NotBeEmpty();
        task.Period.Should().Be(period);
        task.Status.Should().Be(DownloadStatus.Pending);
        task.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        task.StartedAt.Should().BeNull();
        task.CompletedAt.Should().BeNull();
        task.AttemptCount.Should().Be(0);
        task.ErrorMessage.Should().BeNull();
        task.Files.Should().BeEmpty();
        task.HasFiles.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithNullPeriod_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new PeriodTask(null!);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("period");
    }

    [Fact]
    public void Start_FromPendingStatus_ShouldChangeToInProgress()
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);
        var task = new PeriodTask(period);

        // Act
        task.Start();

        // Assert
        task.Status.Should().Be(DownloadStatus.InProgress);
        task.StartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        task.AttemptCount.Should().Be(1);
    }

    [Fact]
    public void Start_WhenAlreadyInProgress_ShouldNotChangeState()
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);
        var task = new PeriodTask(period);
        task.Start();
        var originalStartedAt = task.StartedAt;
        var originalAttemptCount = task.AttemptCount;

        // Act
        task.Start();

        // Assert
        task.Status.Should().Be(DownloadStatus.InProgress);
        task.StartedAt.Should().Be(originalStartedAt);
        task.AttemptCount.Should().Be(originalAttemptCount);
    }

    [Fact]
    public void Complete_FromInProgressStatus_ShouldChangeToCompleted()
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);
        var task = new PeriodTask(period);
        task.Start();

        // Act
        task.Complete();

        // Assert
        task.Status.Should().Be(DownloadStatus.Completed);
        task.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Complete_FromNonInProgressStatus_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);
        var task = new PeriodTask(period);

        // Act & Assert
        var act = () => task.Complete();
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("La tarea no está en progreso");
    }

    [Fact]
    public void Fail_ShouldSetStatusAndErrorMessage()
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);
        var task = new PeriodTask(period);
        var errorMessage = "Error de conexión";

        // Act
        task.Fail(errorMessage);

        // Assert
        task.Status.Should().Be(DownloadStatus.Failed);
        task.ErrorMessage.Should().Be(errorMessage);
        task.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Reset_ShouldRestoreInitialState()
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);
        var task = new PeriodTask(period);
        task.Start();
        task.Fail("Error");

        // Act
        task.Reset();

        // Assert
        task.Status.Should().Be(DownloadStatus.Pending);
        task.StartedAt.Should().BeNull();
        task.CompletedAt.Should().BeNull();
        task.ErrorMessage.Should().BeNull();
        task.Files.Should().BeEmpty();
    }

    [Fact]
    public void AddFile_WithValidFile_ShouldAddToCollection()
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);
        var task = new PeriodTask(period);
        var metadata = new FileMetadata("test.pdf", @"C:\test.pdf", 1024L, FileType.ReciboPdf, DateTime.UtcNow, "hash123");
        var file = new DownloadedFile(period, metadata);

        // Act
        task.AddFile(file);

        // Assert
        task.Files.Should().Contain(file);
        task.HasFiles.Should().BeTrue();
    }

    [Fact]
    public void AddFile_WithNullFile_ShouldThrowArgumentNullException()
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);
        var task = new PeriodTask(period);

        // Act & Assert
        var act = () => task.AddFile(null!);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("file");
    }

    [Theory]
    [InlineData(0, 3, true)]
    [InlineData(2, 3, true)]
    [InlineData(3, 3, false)]
    [InlineData(5, 3, false)]
    public void CanRetry_ShouldReturnCorrectValue(int attemptCount, int maxRetries, bool expectedResult)
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);
        var task = new PeriodTask(period);
        
        // Simulate attempts
        for (int i = 0; i < attemptCount; i++)
        {
            task.Start();
            if (i < attemptCount - 1) // No completar el último intento para evitar estado final
            {
                task.Fail("Simulated failure");
                task.Reset(); // Reset para permitir nuevo intento
            }
        }

        // Act
        var canRetry = task.CanRetry(maxRetries);

        // Assert
        canRetry.Should().Be(expectedResult);
    }

    [Fact]
    public void Duration_WithStartedAndCompletedTask_ShouldReturnCorrectDuration()
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);
        var task = new PeriodTask(period);
        
        task.Start();
        task.Complete();

        // Act
        var duration = task.Duration;

        // Assert
        duration.Should().NotBeNull();
        duration!.Value.Should().BeGreaterOrEqualTo(TimeSpan.Zero);
    }

    [Fact]
    public void Duration_WithNonCompletedTask_ShouldReturnNull()
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);
        var task = new PeriodTask(period);

        // Act
        var duration = task.Duration;

        // Assert
        duration.Should().BeNull();
    }

    [Fact]
    public void Id_ShouldBeUnique()
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);

        // Act
        var task1 = new PeriodTask(period);
        var task2 = new PeriodTask(period);

        // Assert
        task1.Id.Should().NotBe(task2.Id);
    }
}
