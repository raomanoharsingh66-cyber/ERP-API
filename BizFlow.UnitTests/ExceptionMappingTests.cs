using System.Net;
using BizFlow.Application.Common.Exceptions;
using FluentAssertions;
using Xunit;

namespace BizFlow.UnitTests;

public class ExceptionMappingTests
{
    [Fact]
    public void NotFoundException_ShouldSetStatusCode404()
    {
        // Act
        var ex = new NotFoundException("Business", "BIZ-001");

        // Assert
        ex.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Message.Should().Contain("Business");
        ex.Message.Should().Contain("BIZ-001");
    }

    [Fact]
    public void ValidationException_WithErrors_ShouldStoreFailuresAndStatusCode400()
    {
        // Arrange
        var errors = new List<string> { "Email is invalid", "First name is required" };

        // Act
        var ex = new ValidationException(errors);

        // Assert
        ex.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Errors.Should().HaveCount(2).And.Contain("Email is invalid");
    }

    [Fact]
    public void BusinessRuleException_ShouldSetStatusCode422()
    {
        // Act
        var ex = new BusinessRuleException("Cannot deactivate default admin role");

        // Assert
        ex.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        ex.Message.Should().Be("Cannot deactivate default admin role");
    }
}
