using FluentAssertions;
using NominaDownloaderPEIGTO.Domain.ValueObjects;

namespace NominaDownloaderPEIGTO.Tests.Domain.ValueObjects;

public class LoginCredentialsTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange
        var username = "usuario123";
        var password = "password123";

        // Act
        var credentials = new LoginCredentials(username, password);

        // Assert
        credentials.Username.Should().Be(username);
        credentials.Password.Should().Be(password);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidUsername_ShouldThrowArgumentException(string invalidUsername)
    {
        // Act & Assert
        var act = () => new LoginCredentials(invalidUsername, "password123");
            
        act.Should().Throw<ArgumentException>()
           .WithMessage("El nombre de usuario no puede estar vacío*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidPassword_ShouldThrowArgumentException(string invalidPassword)
    {
        // Act & Assert
        var act = () => new LoginCredentials("usuario123", invalidPassword);
            
        act.Should().Throw<ArgumentException>()
           .WithMessage("La contraseña no puede estar vacía*");
    }

    [Fact]
    public void Constructor_WithWhitespaceUsername_ShouldTrimUsername()
    {
        // Arrange
        var username = "  usuario123  ";
        var password = "password123";

        // Act
        var credentials = new LoginCredentials(username, password);

        // Assert
        credentials.Username.Should().Be("usuario123");
    }

    [Fact]
    public void Equality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var credentials1 = new LoginCredentials("usuario123", "password123");
        var credentials2 = new LoginCredentials("usuario123", "password123");

        // Act & Assert
        credentials1.Should().Be(credentials2);
        (credentials1 == credentials2).Should().BeTrue();
        credentials1.GetHashCode().Should().Be(credentials2.GetHashCode());
    }

    [Fact]
    public void Equality_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var credentials1 = new LoginCredentials("usuario123", "password123");
        var credentials2 = new LoginCredentials("usuario456", "password456");

        // Act & Assert
        credentials1.Should().NotBe(credentials2);
        (credentials1 != credentials2).Should().BeTrue();
    }

    [Fact]
    public void ToString_ShouldContainUsername()
    {
        // Arrange
        var credentials = new LoginCredentials("usuario123", "password123");

        // Act
        var result = credentials.ToString();

        // Assert
        result.Should().Contain("usuario123");
    }
}
