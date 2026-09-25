using System.Net;
using System.Text.Json;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Models;
using Serilog;

namespace BizFlow.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(RequestDelegate next, IHostEnvironment env)
    {
        _next = next;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = HttpStatusCode.InternalServerError;
        var message = "An internal server error occurred.";
        var errors = new List<string>();

        switch (exception)
        {
            case ValidationException validationException:
                statusCode = validationException.StatusCode;
                message = validationException.Message;
                errors = validationException.Errors.Any() 
                    ? validationException.Errors 
                    : validationException.Failures.Values.SelectMany(x => x).ToList();
                Log.Warning("Validation failure: {Message}, {@Errors}", message, errors);
                break;

            case NotFoundException notFoundException:
                statusCode = notFoundException.StatusCode;
                message = notFoundException.Message;
                errors = notFoundException.Errors;
                Log.Information("Resource not found: {Message}", message);
                break;

            case BusinessRuleException businessRuleException:
                statusCode = businessRuleException.StatusCode;
                message = businessRuleException.Message;
                errors = businessRuleException.Errors;
                Log.Warning("Business rule violation: {Message}", message);
                break;

            case AppException appException:
                statusCode = appException.StatusCode;
                message = appException.Message;
                errors = appException.Errors;
                Log.Warning("Application error: {Message}", message);
                break;

            default:
                Log.Error(exception, "Unhandled server exception: {Message}", exception.Message);
                if (_env.IsDevelopment())
                {
                    message = exception.Message;
                    if (exception.StackTrace != null)
                    {
                        errors.Add(exception.StackTrace);
                    }
                }
                else
                {
                    message = "An unexpected error occurred. Please contact system support.";
                }
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse.FailureResult(message, errors);
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }
}
