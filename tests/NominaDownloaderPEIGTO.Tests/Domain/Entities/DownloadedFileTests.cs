using FluentAssertions;
using NominaDownloaderPEIGTO.Domain.Entities;
using NominaDownloaderPEIGTO.Domain.Enums;
using NominaDownloaderPEIGTO.Domain.ValueObjects;

namespace NominaDownloaderPEIGTO.Tests.Domain.Entities;

public class DownloadedFileTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);
        var metadata = new FileMetadata(
            "recibo_enero_2024.pdf",
            @"C:\Recibos\2024\01\recibo_enero_2024.pdf",
            1024L,
            FileType.ReciboPdf,
            DateTime.UtcNow,
            "hash123");

        // Act
        var downloadedFile = new DownloadedFile(period, metadata);

        // Assert
        downloadedFile.Id.Should().NotBeEmpty();
        downloadedFile.Period.Should().Be(period);
        downloadedFile.Metadata.Should().Be(metadata);
        downloadedFile.ValidationStatus.Should().Be(ValidationResult.Pending);
        downloadedFile.DownloadedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        downloadedFile.ValidationMessage.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithNullPeriod_ShouldThrowArgumentNullException()
    {
        // Arrange
        var metadata = new FileMetadata(
            "test.pdf",
            @"C:\test.pdf",
            1024L,
            FileType.ReciboPdf,
            DateTime.UtcNow,
            "hash123");

        // Act & Assert
        var act = () => new DownloadedFile(null!, metadata);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("period");
    }

    [Fact]
    public void Constructor_WithNullMetadata_ShouldThrowArgumentNullException()
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);

        // Act & Assert
        var act = () => new DownloadedFile(period, null!);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("metadata");
    }

    [Fact]
    public void MarkAsValid_ShouldSetValidationStatusAndClearMessage()
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);
        var metadata = new FileMetadata("test.pdf", @"C:\test.pdf", 1024L, FileType.ReciboPdf, DateTime.UtcNow, "hash123");
        var downloadedFile = new DownloadedFile(period, metadata);

        // Act
        downloadedFile.MarkAsValid();

        // Assert
        downloadedFile.ValidationStatus.Should().Be(ValidationResult.Valid);
        downloadedFile.ValidationMessage.Should().BeNull();
        downloadedFile.IsValid.Should().BeTrue();
        downloadedFile.RequiresValidation.Should().BeFalse();
    }

    [Fact]
    public void MarkAsInvalid_ShouldSetValidationStatusAndMessage()
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);
        var metadata = new FileMetadata("test.pdf", @"C:\test.pdf", 1024L, FileType.ReciboPdf, DateTime.UtcNow, "hash123");
        var downloadedFile = new DownloadedFile(period, metadata);
        var reason = "Archivo corrupto";

        // Act
        downloadedFile.MarkAsInvalid(reason);

        // Assert
        downloadedFile.ValidationStatus.Should().Be(ValidationResult.Invalid);
        downloadedFile.ValidationMessage.Should().Be(reason);
        downloadedFile.IsValid.Should().BeFalse();
        downloadedFile.RequiresValidation.Should().BeFalse();
    }

    [Fact]
    public void MarkAsCorrupted_ShouldSetValidationStatusAndMessage()
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);
        var metadata = new FileMetadata("test.pdf", @"C:\test.pdf", 1024L, FileType.ReciboPdf, DateTime.UtcNow, "hash123");
        var downloadedFile = new DownloadedFile(period, metadata);
        var reason = "Hash no coincide";

        // Act
        downloadedFile.MarkAsCorrupted(reason);

        // Assert
        downloadedFile.ValidationStatus.Should().Be(ValidationResult.Corrupted);
        downloadedFile.ValidationMessage.Should().Be(reason);
        downloadedFile.IsValid.Should().BeFalse();
        downloadedFile.RequiresValidation.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithPendingStatus_ShouldReturnFalse()
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);
        var metadata = new FileMetadata("test.pdf", @"C:\test.pdf", 1024L, FileType.ReciboPdf, DateTime.UtcNow, "hash123");
        var downloadedFile = new DownloadedFile(period, metadata);

        // Act & Assert
        downloadedFile.IsValid.Should().BeFalse();
    }

    [Fact]
    public void RequiresValidation_WithPendingStatus_ShouldReturnTrue()
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);
        var metadata = new FileMetadata("test.pdf", @"C:\test.pdf", 1024L, FileType.ReciboPdf, DateTime.UtcNow, "hash123");
        var downloadedFile = new DownloadedFile(period, metadata);

        // Act & Assert
        downloadedFile.RequiresValidation.Should().BeTrue();
    }

    [Fact]
    public void RequiresValidation_WithValidStatus_ShouldReturnFalse()
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);
        var metadata = new FileMetadata("test.pdf", @"C:\test.pdf", 1024L, FileType.ReciboPdf, DateTime.UtcNow, "hash123");
        var downloadedFile = new DownloadedFile(period, metadata);
        downloadedFile.MarkAsValid();

        // Act & Assert
        downloadedFile.RequiresValidation.Should().BeFalse();
    }

    [Fact]
    public void DisplayName_ShouldCombinePeriodAndFileName()
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);
        var metadata = new FileMetadata(
            "recibo_enero_2024.pdf",
            @"C:\test.pdf",
            1024L,
            FileType.ReciboPdf,
            DateTime.UtcNow,
            "hash123");
        var downloadedFile = new DownloadedFile(period, metadata);

        // Act
        var displayName = downloadedFile.DisplayName;

        // Assert
        displayName.Should().Contain(period.DisplayName);
        displayName.Should().Contain("recibo_enero_2024.pdf");
    }

    [Fact]
    public void Id_ShouldBeUnique()
    {
        // Arrange
        var period = new PeriodInfo(2024, 1);
        var metadata = new FileMetadata("test.pdf", @"C:\test.pdf", 1024L, FileType.ReciboPdf, DateTime.UtcNow, "hash123");

        // Act
        var file1 = new DownloadedFile(period, metadata);
        var file2 = new DownloadedFile(period, metadata);

        // Assert
        file1.Id.Should().NotBe(file2.Id);
    }
}
