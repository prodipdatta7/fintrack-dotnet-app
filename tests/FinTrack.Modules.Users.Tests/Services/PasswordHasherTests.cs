using FinTrack.Modules.Users.Services;
using FluentAssertions;
using Xunit;

namespace FinTrack.Modules.Users.Tests.Services;

public class PasswordHasherTests
{
    private readonly PasswordHasher _sut = new();

    [Fact]
    public void Hash_WhenPasswordProvided_ReturnsHashedString()
    {
        // Act
        var hash = _sut.Hash("SecretPassword123!");

        // Assert
        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().NotBe("SecretPassword123!");
    }

    [Fact]
    public void Verify_WhenCorrectPassword_ReturnsTrue()
    {
        // Arrange
        var password = "MySecurePassword456!";
        var hash = _sut.Hash(password);

        // Act
        var isValid = _sut.Verify(password, hash);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void Verify_WhenWrongPassword_ReturnsFalse()
    {
        // Arrange
        var hash = _sut.Hash("CorrectPassword");

        // Act
        var isValid = _sut.Verify("WrongPassword", hash);

        // Assert
        isValid.Should().BeFalse();
    }
}
