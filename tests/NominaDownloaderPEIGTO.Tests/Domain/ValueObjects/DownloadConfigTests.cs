using FluentAssertions;
using NominaDownloaderPEIGTO.Domain.Enums;
using NominaDownloaderPEIGTO.Domain.ValueObjects;

namespace NominaDownloaderPEIGTO.Tests.Domain.ValueObjects;

public class DownloadConfigTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange
        var downloadPath = @"C:\Recibos";
        var maxParallelBrowsers = 8;
        var maxRetryAttempts = 3;
        var timeout = TimeSpan.FromMinutes(5);
        var validateDownloads = true;
        var preferredFileType = FileType.ReciboPdf;

        // Act
        var config = new DownloadConfig(
            downloadPath, 
            maxParallelBrowsers, 
            maxRetryAttempts, 
            timeout, 
            validateDownloads, 
            preferredFileType);

        // Assert
        config.DownloadPath.Should().Be(downloadPath);
        config.MaxParallelBrowsers.Should().Be(maxParallelBrowsers);
        config.MaxRetryAttempts.Should().Be(maxRetryAttempts);
        config.TimeoutPerDownload.Should().Be(timeout);
        config.ValidateDownloads.Should().Be(validateDownloads);
        config.PreferredFileType.Should().Be(preferredFileType);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidDownloadPath_ShouldThrowArgumentException(string invalidPath)
    {
        // Act & Assert
        var act = () => new DownloadConfig(invalidPath, 8, 3, TimeSpan.FromMinutes(5), true, FileType.ReciboPdf);
        act.Should().Throw<ArgumentException>()
           .WithMessage("La ruta de descarga no puede estar vacía*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithInvalidMaxParallelBrowsers_ShouldThrowArgumentException(int invalidBrowsers)
    {
        // Act & Assert
        var act = () => new DownloadConfig(@"C:\Recibos", invalidBrowsers, 3, TimeSpan.FromMinutes(5), true, FileType.ReciboPdf);
        act.Should().Throw<ArgumentException>()
           .WithMessage("El número de navegadores paralelos debe ser mayor a 0*");
    }

    [Theory]
    [InlineData(-1)]
    public void Constructor_WithInvalidMaxRetryAttempts_ShouldThrowArgumentException(int invalidRetries)
    {
        // Act & Assert
        var act = () => new DownloadConfig(@"C:\Recibos", 8, invalidRetries, TimeSpan.FromMinutes(5), true, FileType.ReciboPdf);
        act.Should().Throw<ArgumentException>()
           .WithMessage("El número de reintentos no puede ser negativo*");
    }

    [Fact]
    public void Constructor_WithZeroTimeout_ShouldUseDefaultTimeout()
    {
        // Act
        var config = new DownloadConfig(@"C:\Recibos", 8, 3, TimeSpan.Zero, true, FileType.ReciboPdf);
        
        // Assert
        config.TimeoutPerDownload.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void Constructor_WithNegativeTimeout_ShouldKeepNegativeValue()
    {
        // Act
        var config = new DownloadConfig(@"C:\Recibos", 8, 3, TimeSpan.FromSeconds(-1), true, FileType.ReciboPdf);
        
        // Assert
        config.TimeoutPerDownload.Should().Be(TimeSpan.FromSeconds(-1));
    }

    [Fact]
    public void Constructor_WithDefaultValues_ShouldUseDefaults()
    {
        // Arrange
        var downloadPath = @"C:\Downloads";

        // Act
        var config = new DownloadConfig(downloadPath);

        // Assert
        config.DownloadPath.Should().Be(downloadPath);
        config.MaxParallelBrowsers.Should().Be(16);
        config.MaxRetryAttempts.Should().Be(3);
        config.TimeoutPerDownload.Should().Be(TimeSpan.FromMinutes(5));
        config.ValidateDownloads.Should().BeTrue();
        config.PreferredFileType.Should().Be(FileType.ReciboPdf);
    }
}
