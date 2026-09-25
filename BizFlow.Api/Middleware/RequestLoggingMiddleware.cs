using System.Diagnostics;
using BizFlow.Application.Common.Interfaces;
using Serilog;

namespace BizFlow.Api.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public RequestLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentUserService currentUserService)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestMethod = context.Request.Method;
        var requestPath = context.Request.Path;

        try
        {
            await _next(context);
            stopwatch.Stop();

            var statusCode = context.Response.StatusCode;
            var userId = currentUserService.UserId?.ToString() ?? "Anonymous";

            if (statusCode >= 400)
            {
                Log.Warning("HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed}ms [User: {UserId}]",
                    requestMethod, requestPath, statusCode, stopwatch.ElapsedMilliseconds, userId);
            }
            else
            {
                Log.Information("HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed}ms [User: {UserId}]",
                    requestMethod, requestPath, statusCode, stopwatch.ElapsedMilliseconds, userId);
            }
        }
        catch (Exception)
        {
            stopwatch.Stop();
            Log.Error("HTTP {RequestMethod} {RequestPath} failed after {Elapsed}ms",
                requestMethod, requestPath, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
