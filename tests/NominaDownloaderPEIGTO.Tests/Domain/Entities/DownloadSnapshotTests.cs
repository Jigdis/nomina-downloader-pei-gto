using FluentAssertions;
using NominaDownloaderPEIGTO.Domain.Entities;
using NominaDownloaderPEIGTO.Domain.ValueObjects;

namespace NominaDownloaderPEIGTO.Tests.Domain.Entities;

public class DownloadSnapshotTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var periods = new List<PeriodInfo>
        {
            new PeriodInfo(2024, 1),
            new PeriodInfo(2024, 2)
        };
        var downloadPath = @"C:\Downloads";

        // Act
        var snapshot = new DownloadSnapshot(sessionId, periods, downloadPath);

        // Assert
        snapshot.Id.Should().NotBeEmpty();
        snapshot.SessionId.Should().Be(sessionId);
        snapshot.RequestedPeriods.Should().HaveCount(2);
        snapshot.RequestedPeriods.Should().BeEquivalentTo(periods);
        snapshot.DownloadPath.Should().Be(downloadPath);
        snapshot.InitialFolderState.Should().BeEmpty();
        snapshot.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_WithNullSessionId_ShouldNotThrow()
    {
        // Arrange
        var periods = new List<PeriodInfo> { new PeriodInfo(2024, 1) };
        var downloadPath = @"C:\Downloads";

        // Act
        var act = () => new DownloadSnapshot(Guid.Empty, periods, downloadPath);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithNullPeriods_ShouldThrowArgumentNullException()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var downloadPath = @"C:\Downloads";

        // Act & Assert
        var act = () => new DownloadSnapshot(sessionId, null!, downloadPath);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("requestedPeriods");
    }

    [Fact]
    public void Constructor_WithNullDownloadPath_ShouldThrowArgumentNullException()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var periods = new List<PeriodInfo> { new PeriodInfo(2024, 1) };

        // Act & Assert
        var act = () => new DownloadSnapshot(sessionId, periods, null!);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("downloadPath");
    }

    [Fact]
    public void Constructor_WithEmptyPeriods_ShouldCreateInstance()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var periods = new List<PeriodInfo>();
        var downloadPath = @"C:\Downloads";

        // Act
        var snapshot = new DownloadSnapshot(sessionId, periods, downloadPath);

        // Assert
        snapshot.RequestedPeriods.Should().BeEmpty();
        snapshot.InitialFolderState.Should().BeEmpty();
    }

    [Fact]
    public void RequestedPeriods_ShouldBeDefensiveCopy()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var periods = new List<PeriodInfo> { new PeriodInfo(2024, 1) };
        var downloadPath = @"C:\Downloads";

        // Act
        var snapshot = new DownloadSnapshot(sessionId, periods, downloadPath);
        periods.Add(new PeriodInfo(2024, 2)); // Modify original list

        // Assert
        snapshot.RequestedPeriods.Should().HaveCount(1);
    }

    [Fact]
    public void CaptureInitialState_ShouldClearAndPopulateFolderState()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var periods = new List<PeriodInfo>
        {
            new PeriodInfo(2024, 1),
            new PeriodInfo(2024, 2)
        };
        var downloadPath = @"C:\Downloads";
        var snapshot = new DownloadSnapshot(sessionId, periods, downloadPath);

        // Act
        snapshot.CaptureInitialState();

        // Assert
        snapshot.InitialFolderState.Should().HaveCount(2);
        snapshot.InitialFolderState.Keys.Should().Contain("2024-01");
        snapshot.InitialFolderState.Keys.Should().Contain("2024-02");
    }

    [Fact]
    public void GetEmptyFolders_WithNonExistingFolders_ShouldReturnEmptyList()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var periods = new List<PeriodInfo> { new PeriodInfo(2024, 1) };
        var downloadPath = @"C:\NonExistingPath";
        var snapshot = new DownloadSnapshot(sessionId, periods, downloadPath);
        snapshot.CaptureInitialState();

        // Act
        var emptyFolders = snapshot.GetEmptyFolders();

        // Assert
        emptyFolders.Should().BeEmpty();
    }

    [Fact]
    public void GetPeriodsForEmptyFolders_ShouldReturnPeriodsWithEmptyFolders()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var periods = new List<PeriodInfo>
        {
            new PeriodInfo(2024, 1),
            new PeriodInfo(2024, 2)
        };
        var downloadPath = @"C:\NonExistingPath";
        var snapshot = new DownloadSnapshot(sessionId, periods, downloadPath);
        snapshot.CaptureInitialState();

        // Act
        var emptyPeriods = snapshot.GetPeriodsForEmptyFolders();

        // Assert
        emptyPeriods.Should().BeEmpty(); // No folders exist, so none are "empty"
    }

    [Fact]
    public void Id_ShouldBeUnique()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var periods = new List<PeriodInfo> { new PeriodInfo(2024, 1) };
        var downloadPath = @"C:\Downloads";

        // Act
        var snapshot1 = new DownloadSnapshot(sessionId, periods, downloadPath);
        var snapshot2 = new DownloadSnapshot(sessionId, periods, downloadPath);

        // Assert
        snapshot1.Id.Should().NotBe(snapshot2.Id);
    }
}

public class FolderSnapshotTests
{
    [Fact]
    public void Constructor_WithValidPath_ShouldCreateInstance()
    {
        // Arrange
        var folderPath = @"C:\TestFolder";

        // Act
        var snapshot = new FolderSnapshot(folderPath);

        // Assert
        snapshot.FolderPath.Should().Be(folderPath);
        snapshot.ExistingFiles.Should().NotBeNull();
        snapshot.CapturedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_WithNullPath_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new FolderSnapshot(null!);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("folderPath");
    }

    [Fact]
    public void Constructor_WithNonExistingFolder_ShouldCreateInstanceWithEmptyFileList()
    {
        // Arrange
        var folderPath = @"C:\NonExistingFolder";

        // Act
        var snapshot = new FolderSnapshot(folderPath);

        // Assert
        snapshot.FolderPath.Should().Be(folderPath);
        snapshot.ExistingFiles.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithEmptyPath_ShouldNotThrow()
    {
        // Arrange
        var folderPath = "";

        // Act
        var act = () => new FolderSnapshot(folderPath);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ExistingFiles_ShouldBeInitializedEmpty()
    {
        // Arrange
        var folderPath = @"C:\TestFolder";

        // Act
        var snapshot = new FolderSnapshot(folderPath);

        // Assert
        snapshot.ExistingFiles.Should().NotBeNull();
        snapshot.ExistingFiles.Should().BeOfType<List<string>>();
    }
}
