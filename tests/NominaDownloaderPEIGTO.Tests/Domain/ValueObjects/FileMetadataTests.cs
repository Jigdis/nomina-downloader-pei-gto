using FluentAssertions;
using NominaDownloaderPEIGTO.Domain.Enums;
using NominaDownloaderPEIGTO.Domain.ValueObjects;

namespace NominaDownloaderPEIGTO.Tests.Domain.ValueObjects;

public class FileMetadataTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange
        var fileName = "recibo_enero_2024.pdf";
        var filePath = @"C:\Recibos\2024\01\recibo_enero_2024.pdf";
        var fileSize = 1024L;
        var fileType = FileType.ReciboPdf;
        var downloadedAt = DateTime.UtcNow;
        var hash = "abc123";

        // Act
        var metadata = new FileMetadata(fileName, filePath, fileSize, fileType, downloadedAt, hash);

        // Assert
        metadata.FileName.Should().Be(fileName);
        metadata.FilePath.Should().Be(filePath);
        metadata.FileSize.Should().Be(fileSize);
        metadata.FileType.Should().Be(fileType);
        metadata.DownloadedAt.Should().Be(downloadedAt);
        metadata.Hash.Should().Be(hash);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidFileName_ShouldThrowArgumentException(string invalidFileName)
    {
        // Act & Assert
        var act = () => new FileMetadata(
            invalidFileName, 
            @"C:\test.pdf", 
            1024L, 
            FileType.ReciboPdf, 
            DateTime.UtcNow, 
            "hash123");
            
        act.Should().Throw<ArgumentException>()
           .WithMessage("El nombre del archivo no puede estar vacío*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidFilePath_ShouldThrowArgumentException(string invalidFilePath)
    {
        // Act & Assert
        var act = () => new FileMetadata(
            "test.pdf", 
            invalidFilePath, 
            1024L, 
            FileType.ReciboPdf, 
            DateTime.UtcNow, 
            "hash123");
            
        act.Should().Throw<ArgumentException>()
           .WithMessage("La ruta del archivo no puede estar vacía*");
    }

    [Fact]
    public void Constructor_WithNegativeFileSize_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = () => new FileMetadata(
            "test.pdf", 
            @"C:\test.pdf", 
            -1L, 
            FileType.ReciboPdf, 
            DateTime.UtcNow, 
            "hash123");
            
        act.Should().Throw<ArgumentException>()
           .WithMessage("El tamaño del archivo no puede ser negativo*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidHash_ShouldThrowArgumentException(string invalidHash)
    {
        // Act & Assert
        var act = () => new FileMetadata(
            "test.pdf", 
            @"C:\test.pdf", 
            1024L, 
            FileType.ReciboPdf, 
            DateTime.UtcNow, 
            invalidHash);
            
        act.Should().Throw<ArgumentException>()
           .WithMessage("El hash del archivo no puede estar vacío*");
    }

    [Fact]
    public void IsValid_WithValidData_ShouldReturnTrue()
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
        metadata.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithZeroFileSize_ShouldReturnFalse()
    {
        // Arrange
        var metadata = new FileMetadata(
            "test.pdf", 
            @"C:\test.pdf", 
            0L, 
            FileType.ReciboPdf, 
            DateTime.UtcNow, 
            "hash123");

        // Act & Assert
        metadata.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Equality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var downloadedAt = DateTime.UtcNow;
        var metadata1 = new FileMetadata("test.pdf", @"C:\test.pdf", 1024L, FileType.ReciboPdf, downloadedAt, "hash123");
        var metadata2 = new FileMetadata("test.pdf", @"C:\test.pdf", 1024L, FileType.ReciboPdf, downloadedAt, "hash123");

        // Act & Assert
        metadata1.Should().Be(metadata2);
        (metadata1 == metadata2).Should().BeTrue();
        metadata1.GetHashCode().Should().Be(metadata2.GetHashCode());
    }

    [Fact]
    public void Equality_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var downloadedAt = DateTime.UtcNow;
        var metadata1 = new FileMetadata("test1.pdf", @"C:\test1.pdf", 1024L, FileType.ReciboPdf, downloadedAt, "hash123");
        var metadata2 = new FileMetadata("test2.pdf", @"C:\test2.pdf", 2048L, FileType.CfdiXml, downloadedAt, "hash456");

        // Act & Assert
        metadata1.Should().NotBe(metadata2);
        (metadata1 != metadata2).Should().BeTrue();
    }
}
