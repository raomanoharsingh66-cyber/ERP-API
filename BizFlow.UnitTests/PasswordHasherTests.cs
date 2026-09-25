using BizFlow.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace BizFlow.UnitTests;

public class PasswordHasherTests
{
    private readonly PasswordHasher _sut = new();

    [Fact]
    public void HashPassword_ShouldProduceValidDelimitedHash()
    {
        // Arrange
        var password = "SecurePassword@123";

        // Act
        var hash = _sut.HashPassword(password);

        // Assert
        hash.Should().NotBeNullOrWhiteSpace();
        var parts = hash.Split(';');
        parts.Should().HaveCount(4, "Hash format should be salt;hash;iterations;algorithm");
        parts[2].Should().Be("100000");
        parts[3].Should().Be("SHA256");
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        var password = "CorrectHorseBatteryStaple!";
        var hash = _sut.HashPassword(password);

        // Act
        var isValid = _sut.VerifyPassword(password, hash);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithWrongPassword_ShouldReturnFalse()
    {
        // Arrange
        var password = "CorrectPassword123";
        var wrongPassword = "WrongPassword456";
        var hash = _sut.HashPassword(password);

        // Act
        var isValid = _sut.VerifyPassword(wrongPassword, hash);

        // Assert
        isValid.Should().BeFalse();
    }
}
