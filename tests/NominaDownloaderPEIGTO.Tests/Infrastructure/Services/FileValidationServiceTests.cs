using FluentAssertions;
using Moq;
using NominaDownloaderPEIGTO.Domain.Enums;
using NominaDownloaderPEIGTO.Infrastructure.Services;

namespace NominaDownloaderPEIGTO.Tests.Infrastructure.Services;

public class FileValidationServiceTests : IDisposable
{
    private readonly FileValidationService _service;
    private readonly string _testDirectory;

    public FileValidationServiceTests()
    {
        _service = new FileValidationService();
        _testDirectory = Path.Combine(Path.GetTempPath(), "FileValidationTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public async Task ValidateFileAsync_WithValidPdfFile_ShouldReturnCorrectMetadata()
    {
        // Arrange
        var fileName = "test.pdf";
        var filePath = Path.Combine(_testDirectory, fileName);
        var testContent = "PDF test content";
        await File.WriteAllTextAsync(filePath, testContent);

        // Act
        var metadata = await _service.ValidateFileAsync(filePath);

        // Assert
        metadata.Should().NotBeNull();
        metadata.FileName.Should().Be(fileName);
        metadata.FilePath.Should().Be(filePath);
        metadata.FileSize.Should().Be(testContent.Length);
        metadata.FileType.Should().Be(FileType.ReciboPdf);
        metadata.Hash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ValidateFileAsync_WithValidXmlFile_ShouldReturnCorrectMetadata()
    {
        // Arrange
        var fileName = "test.xml";
        var filePath = Path.Combine(_testDirectory, fileName);
        var testContent = "<xml>test content</xml>";
        await File.WriteAllTextAsync(filePath, testContent);

        // Act
        var metadata = await _service.ValidateFileAsync(filePath);

        // Assert
        metadata.Should().NotBeNull();
        metadata.FileName.Should().Be(fileName);
        metadata.FilePath.Should().Be(filePath);
        metadata.FileSize.Should().Be(testContent.Length);
        metadata.FileType.Should().Be(FileType.CfdiXml);
        metadata.Hash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ValidateFileAsync_WithNonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "nonexistent.pdf");

        // Act & Assert
        var act = async () => await _service.ValidateFileAsync(filePath);
        await act.Should().ThrowAsync<FileNotFoundException>()
                .WithMessage($"Archivo no encontrado: {filePath}");
    }

    [Fact]
    public async Task FileExistsAsync_WithExistingFile_ShouldReturnTrue()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "existing.txt");
        await File.WriteAllTextAsync(filePath, "test");

        // Act
        var exists = await _service.FileExistsAsync(filePath);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task FileExistsAsync_WithNonExistentFile_ShouldReturnFalse()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "nonexistent.txt");

        // Act
        var exists = await _service.FileExistsAsync(filePath);

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task CalculateFileHashAsync_WithSameContent_ShouldReturnSameHash()
    {
        // Arrange
        var content = "test content for hashing";
        var filePath1 = Path.Combine(_testDirectory, "file1.txt");
        var filePath2 = Path.Combine(_testDirectory, "file2.txt");
        
        await File.WriteAllTextAsync(filePath1, content);
        await File.WriteAllTextAsync(filePath2, content);

        // Act
        var hash1 = await _service.CalculateFileHashAsync(filePath1);
        var hash2 = await _service.CalculateFileHashAsync(filePath2);

        // Assert
        hash1.Should().Be(hash2);
        hash1.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CalculateFileHashAsync_WithDifferentContent_ShouldReturnDifferentHash()
    {
        // Arrange
        var filePath1 = Path.Combine(_testDirectory, "file1.txt");
        var filePath2 = Path.Combine(_testDirectory, "file2.txt");
        
        await File.WriteAllTextAsync(filePath1, "content 1");
        await File.WriteAllTextAsync(filePath2, "content 2");

        // Act
        var hash1 = await _service.CalculateFileHashAsync(filePath1);
        var hash2 = await _service.CalculateFileHashAsync(filePath2);

        // Assert
        hash1.Should().NotBe(hash2);
        hash1.Should().NotBeNullOrEmpty();
        hash2.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetFileSizeAsync_ShouldReturnCorrectSize()
    {
        // Arrange
        var content = "test content with specific length";
        var filePath = Path.Combine(_testDirectory, "sizetest.txt");
        await File.WriteAllTextAsync(filePath, content);

        // Act
        var size = await _service.GetFileSizeAsync(filePath);

        // Assert
        size.Should().Be(content.Length);
    }

    [Fact]
    public async Task ValidateFileAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.pdf");
        await File.WriteAllTextAsync(filePath, "test content");
        
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var act = async () => await _service.ValidateFileAsync(filePath, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Theory]
    [InlineData(".pdf", FileType.ReciboPdf)]
    [InlineData(".PDF", FileType.ReciboPdf)]
    [InlineData(".xml", FileType.CfdiXml)]
    [InlineData(".XML", FileType.CfdiXml)]
    public async Task ValidateFileAsync_WithDifferentExtensions_ShouldReturnCorrectFileType(string extension, FileType expectedFileType)
    {
        // Arrange
        var fileName = $"test{extension}";
        var filePath = Path.Combine(_testDirectory, fileName);
        await File.WriteAllTextAsync(filePath, "test content");

        // Act
        var metadata = await _service.ValidateFileAsync(filePath);

        // Assert
        metadata.FileType.Should().Be(expectedFileType);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
}
