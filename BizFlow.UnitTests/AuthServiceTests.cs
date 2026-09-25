using BizFlow.Application.DTOs.Auth;
using BizFlow.Application.Validators.Auth;
using FluentAssertions;
using Xunit;

namespace BizFlow.UnitTests;

public class AuthServiceTests
{
    private readonly RegisterBusinessValidator _registerValidator = new();
    private readonly LoginRequestValidator _loginValidator = new();

    [Fact]
    public void RegisterBusinessValidator_WithValidData_ShouldPass()
    {
        // Arrange
        var dto = new RegisterBusinessDto
        {
            BusinessName = "Acme Global Industries",
            BusinessCode = "ACME-IND",
            BusinessEmail = "info@acmeind.com",
            AdminFirstName = "John",
            AdminLastName = "Doe",
            AdminEmail = "john.doe@acmeind.com",
            AdminPassword = "SecurePassword@123",
            Currency = "INR"
        };

        // Act
        var result = _registerValidator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RegisterBusinessValidator_WithEmptyCode_ShouldFail()
    {
        // Arrange
        var dto = new RegisterBusinessDto
        {
            BusinessName = "Acme Global",
            BusinessCode = "",
            BusinessEmail = "info@acme.com",
            AdminFirstName = "John",
            AdminLastName = "Doe",
            AdminEmail = "john@acme.com",
            AdminPassword = "SecurePassword@123"
        };

        // Act
        var result = _registerValidator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BusinessCode");
    }

    [Fact]
    public void RegisterBusinessValidator_WithInvalidPassword_ShouldFail()
    {
        // Arrange
        var dto = new RegisterBusinessDto
        {
            BusinessName = "Acme Global",
            BusinessCode = "ACME-01",
            BusinessEmail = "info@acme.com",
            AdminFirstName = "John",
            AdminLastName = "Doe",
            AdminEmail = "john@acme.com",
            AdminPassword = "short" // < 8 chars and missing required criteria
        };

        // Act
        var result = _registerValidator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AdminPassword");
    }

    [Fact]
    public void LoginRequestValidator_WithInvalidEmail_ShouldFail()
    {
        // Arrange
        var dto = new LoginRequestDto
        {
            Email = "not-an-email",
            Password = "Password123"
        };

        // Act
        var result = _loginValidator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }
}
