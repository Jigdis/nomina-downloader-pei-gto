using FluentAssertions;
using NominaDownloaderPEIGTO.Domain.Entities;
using NominaDownloaderPEIGTO.Domain.Enums;
using NominaDownloaderPEIGTO.Domain.ValueObjects;

namespace NominaDownloaderPEIGTO.Tests.Domain.Entities;

public class DownloadSessionTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "pass");
        var config = new DownloadConfig(@"C:\Downloads");

        // Act
        var session = new DownloadSession(credentials, config);

        // Assert
        session.Id.Should().NotBeEmpty();
        session.Credentials.Should().Be(credentials);
        session.Config.Should().Be(config);
        session.StartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        session.CompletedAt.Should().BeNull();
        session.Status.Should().Be(DownloadStatus.Pending);
        session.PeriodTasks.Should().BeEmpty();
        session.DownloadedFiles.Should().BeEmpty();
        session.FailedTasks.Should().BeEmpty();
        session.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithNullCredentials_ShouldThrowArgumentNullException()
    {
        // Arrange
        var config = new DownloadConfig(@"C:\Downloads");

        // Act & Assert
        var act = () => new DownloadSession(null!, config);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("credentials");
    }

    [Fact]
    public void Constructor_WithNullConfig_ShouldThrowArgumentNullException()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "pass");

        // Act & Assert
        var act = () => new DownloadSession(credentials, null!);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("config");
    }

    [Fact]
    public void AddPeriodTask_WithValidPeriod_ShouldAddTask()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "pass");
        var config = new DownloadConfig(@"C:\Downloads");
        var session = new DownloadSession(credentials, config);
        var period = new PeriodInfo(2024, 1);

        // Act
        session.AddPeriodTask(period);

        // Assert
        session.PeriodTasks.Should().HaveCount(1);
        session.PeriodTasks.First().Period.Should().Be(period);
        session.TotalTasks.Should().Be(1);
    }

    [Fact]
    public void AddPeriodTask_WithNullPeriod_ShouldThrowArgumentNullException()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "pass");
        var config = new DownloadConfig(@"C:\Downloads");
        var session = new DownloadSession(credentials, config);

        // Act & Assert
        var act = () => session.AddPeriodTask(null!);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("period");
    }

    [Fact]
    public void AddPeriodTask_WithDuplicatePeriod_ShouldNotAddDuplicate()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "pass");
        var config = new DownloadConfig(@"C:\Downloads");
        var session = new DownloadSession(credentials, config);
        var period = new PeriodInfo(2024, 1);

        // Act
        session.AddPeriodTask(period);
        session.AddPeriodTask(period); // Duplicate

        // Assert
        session.PeriodTasks.Should().HaveCount(1);
    }

    [Fact]
    public void Start_FromPendingStatus_ShouldChangeToInProgress()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "pass");
        var config = new DownloadConfig(@"C:\Downloads");
        var session = new DownloadSession(credentials, config);

        // Act
        session.Start();

        // Assert
        session.Status.Should().Be(DownloadStatus.InProgress);
    }

    [Fact]
    public void Start_WhenAlreadyStarted_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "pass");
        var config = new DownloadConfig(@"C:\Downloads");
        var session = new DownloadSession(credentials, config);
        session.Start();

        // Act & Assert
        var act = () => session.Start();
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("La sesión ya fue iniciada");
    }

    [Fact]
    public void Complete_FromInProgressStatus_ShouldChangeToCompleted()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "pass");
        var config = new DownloadConfig(@"C:\Downloads");
        var session = new DownloadSession(credentials, config);
        session.Start();

        // Act
        session.Complete();

        // Assert
        session.Status.Should().Be(DownloadStatus.Completed);
        session.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Complete_FromNonInProgressStatus_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "pass");
        var config = new DownloadConfig(@"C:\Downloads");
        var session = new DownloadSession(credentials, config);

        // Act & Assert
        var act = () => session.Complete();
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("La sesión no está en progreso");
    }

    [Fact]
    public void Fail_ShouldSetStatusAndErrorMessage()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "pass");
        var config = new DownloadConfig(@"C:\Downloads");
        var session = new DownloadSession(credentials, config);
        var errorMessage = "Connection failed";

        // Act
        session.Fail(errorMessage);

        // Assert
        session.Status.Should().Be(DownloadStatus.Failed);
        session.ErrorMessage.Should().Be(errorMessage);
        session.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void AddDownloadedFile_WithValidFile_ShouldAddToCollection()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "pass");
        var config = new DownloadConfig(@"C:\Downloads");
        var session = new DownloadSession(credentials, config);
        var period = new PeriodInfo(2024, 1);
        var metadata = new FileMetadata("test.pdf", @"C:\test.pdf", 1024L, FileType.ReciboPdf, DateTime.UtcNow, "hash123");
        var downloadedFile = new DownloadedFile(period, metadata);

        // Act
        session.AddDownloadedFile(downloadedFile);

        // Assert
        session.DownloadedFiles.Should().Contain(downloadedFile);
        session.SuccessfulDownloads.Should().Be(1);
    }

    [Fact]
    public void AddDownloadedFile_WithNullFile_ShouldThrowArgumentNullException()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "pass");
        var config = new DownloadConfig(@"C:\Downloads");
        var session = new DownloadSession(credentials, config);

        // Act & Assert
        var act = () => session.AddDownloadedFile(null!);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("file");
    }

    [Fact]
    public void AddFailedTask_WithValidTask_ShouldAddToCollection()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "pass");
        var config = new DownloadConfig(@"C:\Downloads");
        var session = new DownloadSession(credentials, config);
        var period = new PeriodInfo(2024, 1);
        var failedTask = new FailedTask(period, "Download failed", 1);

        // Act
        session.AddFailedTask(failedTask);

        // Assert
        session.FailedTasks.Should().Contain(failedTask);
        session.FailedTasksCount.Should().Be(1);
    }

    [Fact]
    public void AddFailedTask_WithNullTask_ShouldThrowArgumentNullException()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "pass");
        var config = new DownloadConfig(@"C:\Downloads");
        var session = new DownloadSession(credentials, config);

        // Act & Assert
        var act = () => session.AddFailedTask(null!);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("failedTask");
    }

    [Fact]
    public void ProgressPercentage_WithNoTasks_ShouldReturnZero()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "pass");
        var config = new DownloadConfig(@"C:\Downloads");
        var session = new DownloadSession(credentials, config);

        // Act & Assert
        session.ProgressPercentage.Should().Be(0);
    }

    [Fact]
    public void ProgressPercentage_WithMixedTasks_ShouldReturnCorrectPercentage()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "pass");
        var config = new DownloadConfig(@"C:\Downloads");
        var session = new DownloadSession(credentials, config);
        
        var period1 = new PeriodInfo(2024, 1);
        var period2 = new PeriodInfo(2024, 2);
        var period3 = new PeriodInfo(2024, 3);
        
        session.AddPeriodTask(period1);
        session.AddPeriodTask(period2);
        session.AddPeriodTask(period3);

        // Complete one task
        session.PeriodTasks.First().Start();
        session.PeriodTasks.First().Complete();

        // Act & Assert
        session.ProgressPercentage.Should().BeApproximately(33.33, 0.01);
    }

    [Fact]
    public void Duration_WithCompletedSession_ShouldReturnTimeSpan()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "pass");
        var config = new DownloadConfig(@"C:\Downloads");
        var session = new DownloadSession(credentials, config);
        session.Start();
        
        session.Complete();

        // Act
        var duration = session.Duration;

        // Assert
        duration.Should().NotBeNull();
        duration!.Value.Should().BeGreaterOrEqualTo(TimeSpan.Zero);
    }

    [Fact]
    public void Duration_WithIncompleteSession_ShouldReturnNull()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "pass");
        var config = new DownloadConfig(@"C:\Downloads");
        var session = new DownloadSession(credentials, config);

        // Act
        var duration = session.Duration;

        // Assert
        duration.Should().BeNull();
    }

    [Fact]
    public void Id_ShouldBeUnique()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "pass");
        var config = new DownloadConfig(@"C:\Downloads");

        // Act
        var session1 = new DownloadSession(credentials, config);
        var session2 = new DownloadSession(credentials, config);

        // Assert
        session1.Id.Should().NotBe(session2.Id);
    }
}
