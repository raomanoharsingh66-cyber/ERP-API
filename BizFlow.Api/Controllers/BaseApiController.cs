using BizFlow.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace BizFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected ActionResult<ApiResponse<T>> OkResponse<T>(T data, string message = "Operation completed successfully.")
    {
        return Ok(ApiResponse<T>.SuccessResult(data, message));
    }

    protected ActionResult<ApiResponse> OkResponse(string message = "Operation completed successfully.")
    {
        return Ok(ApiResponse.SuccessResult(message));
    }

    protected ActionResult<ApiResponse<T>> CreatedResponse<T>(
        string actionName, 
        object? routeValues, 
        T data, 
        string message = "Resource created successfully.")
    {
        return CreatedAtAction(actionName, routeValues, ApiResponse<T>.SuccessResult(data, message));
    }

    protected ActionResult<ApiResponse> BadRequestResponse(string message, List<string>? errors = null)
    {
        return BadRequest(ApiResponse.FailureResult(message, errors));
    }

    protected ActionResult<ApiResponse<T>> BadRequestResponse<T>(string message, List<string>? errors = null)
    {
        return BadRequest(ApiResponse<T>.FailureResult(message, errors));
    }

    protected ActionResult<ApiResponse> NotFoundResponse(string message = "Resource not found.")
    {
        return NotFound(ApiResponse.FailureResult(message));
    }

    protected ActionResult<ApiResponse<T>> NotFoundResponse<T>(string message = "Resource not found.")
    {
        return NotFound(ApiResponse<T>.FailureResult(message));
    }
}
