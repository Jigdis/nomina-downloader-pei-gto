using FluentAssertions;
using NominaDownloaderPEIGTO.Domain.ValueObjects;

namespace NominaDownloaderPEIGTO.Tests.Domain.ValueObjects;

public class PeriodInfoTests
{
    [Fact]
    public void Constructor_WithValidYearAndPeriod_ShouldCreateInstance()
    {
        // Arrange
        var year = 2024;
        var period = 6;

        // Act
        var periodInfo = new PeriodInfo(year, period);

        // Assert
        periodInfo.Year.Should().Be(year);
        periodInfo.Period.Should().Be(period);
        periodInfo.DisplayName.Should().Be("Período 06: Junio");
    }

    [Fact]
    public void Constructor_WithValidYearPeriodAndDescription_ShouldCreateInstance()
    {
        // Arrange
        var year = 2024;
        var period = 12;
        var description = "DICIEMBRE";

        // Act
        var periodInfo = new PeriodInfo(year, period, description);

        // Assert
        periodInfo.Year.Should().Be(year);
        periodInfo.Period.Should().Be(period);
        periodInfo.Description.Should().Be(description);
        periodInfo.DisplayName.Should().Be("Período 12: DICIEMBRE");
    }

    [Theory]
    [InlineData(13)]
    [InlineData(-1)]
    public void Constructor_WithInvalidPeriod_ShouldThrowArgumentException(int invalidPeriod)
    {
        // Arrange
        var year = 2024;

        // Act & Assert
        var act = () => new PeriodInfo(year, invalidPeriod);
        act.Should().Throw<ArgumentException>()
           .WithMessage("Mes inválido");
    }

    [Fact]
    public void Constructor_WithPeriodZero_ShouldCreateComplementariaInstance()
    {
        // Arrange
        var year = 2024;
        var period = 0;

        // Act
        var periodInfo = new PeriodInfo(year, period);

        // Assert
        periodInfo.Year.Should().Be(year);
        periodInfo.Period.Should().Be(period);
        periodInfo.DisplayName.Should().Be("Período 00: Complementaría");
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(2050)]
    [InlineData(0)]
    public void Constructor_WithInvalidYear_ShouldThrowArgumentException(int invalidYear)
    {
        // Arrange
        var period = 6;

        // Act & Assert
        var act = () => new PeriodInfo(invalidYear, period);
        act.Should().Throw<ArgumentException>()
           .WithMessage($"El año debe estar entre 2000 y {DateTime.Now.Year + 1}*");
    }

    [Fact]
    public void Equals_WithSameYearAndPeriod_ShouldReturnTrue()
    {
        // Arrange
        var period1 = new PeriodInfo(2024, 6);
        var period2 = new PeriodInfo(2024, 6);

        // Act & Assert
        period1.Should().Be(period2);
        period1.GetHashCode().Should().Be(period2.GetHashCode());
    }

    [Fact]
    public void Equals_WithDifferentYearOrPeriod_ShouldReturnFalse()
    {
        // Arrange
        var period1 = new PeriodInfo(2024, 6);
        var period2 = new PeriodInfo(2024, 7);
        var period3 = new PeriodInfo(2023, 6);

        // Act & Assert
        period1.Should().NotBe(period2);
        period1.Should().NotBe(period3);
    }

    [Fact]
    public void ToString_ShouldReturnDisplayName()
    {
        // Arrange
        var periodInfo = new PeriodInfo(2024, 6, "JUNIO");

        // Act
        var result = periodInfo.ToString();

        // Assert
        result.Should().Be("Período 06: JUNIO");
    }
}
