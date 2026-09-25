using BizFlow.Application.Common.Models;
using FluentAssertions;
using Xunit;

namespace BizFlow.UnitTests;

public class ApiResponseTests
{
    [Fact]
    public void SuccessResult_ShouldReturnSuccessTrueAndData()
    {
        // Arrange
        var testData = new { Name = "BizFlow", Modules = 5 };

        // Act
        var response = ApiResponse<object>.SuccessResult(testData, "Custom success message");

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.Message.Should().Be("Custom success message");
        response.Data.Should().BeEquivalentTo(testData);
        response.Errors.Should().BeEmpty();
    }

    [Fact]
    public void FailureResult_ShouldReturnSuccessFalseAndErrors()
    {
        // Arrange
        var errorList = new List<string> { "Field is required", "Field must be numeric" };

        // Act
        var response = ApiResponse.FailureResult("Validation failed", errorList);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeFalse();
        response.Message.Should().Be("Validation failed");
        response.Data.Should().BeNull();
        response.Errors.Should().HaveCount(2).And.Contain("Field is required");
    }

    [Fact]
    public void FailureResult_WithSingleError_ShouldContainErrorInList()
    {
        // Act
        var response = ApiResponse.FailureResult("Entity not found", "Item with ID 100 was not found");

        // Assert
        response.Success.Should().BeFalse();
        response.Errors.Should().ContainSingle().Which.Should().Be("Item with ID 100 was not found");
    }
}
