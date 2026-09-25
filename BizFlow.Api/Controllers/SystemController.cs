using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BizFlow.Api.Controllers;

[AllowAnonymous]
public class SystemController : BaseApiController
{
    private readonly IApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SystemController(
        IApplicationDbContext context,
        IWebHostEnvironment environment,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _environment = environment;
        _dateTimeProvider = dateTimeProvider;
    }

    [HttpGet("health")]
    public ActionResult<ApiResponse<SystemHealthDto>> GetHealth()
    {
        var healthDto = new SystemHealthDto
        {
            Status = "Healthy",
            Environment = _environment.EnvironmentName,
            ServerTime = _dateTimeProvider.UtcNow,
            Version = "1.0.0",
            DatabaseProvider = "SQL Server 2022"
        };

        return OkResponse(healthDto, "BizFlow ERP API is operational.");
    }

    [HttpGet("db-check")]
    public async Task<ActionResult<ApiResponse<object>>> CheckDatabase()
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync();

            if (canConnect)
            {
                return OkResponse<object>(new
                {
                    Connected = true,
                    Provider = _context.Database.ProviderName,
                    Status = "SQL Server database connected successfully."
                }, "Database connection verified.");
            }

            return BadRequestResponse<object>("Unable to connect to SQL Server database.", new List<string>
            {
                "Database connection test returned false."
            });
        }
        catch (Exception ex)
        {
            return BadRequestResponse<object>("Database connection failed.", new List<string> { ex.Message });
        }
    }
}
