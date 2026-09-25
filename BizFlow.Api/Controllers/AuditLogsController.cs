using BizFlow.Api.Attributes;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Dashboard;
using BizFlow.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace BizFlow.Api.Controllers;

[Route("api/[controller]")]
[Route("api/audit-logs")]
public class AuditLogsController : BaseApiController
{
    private readonly IDashboardService _dashboardService;

    public AuditLogsController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    [HasPermission(Permissions.Business.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<AuditLogDto>>>> GetAuditLogs(
        [FromQuery] string? search,
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _dashboardService.GetAuditLogsAsync(search, fromDate, toDate, page, pageSize, cancellationToken);
        return Ok(ApiResponse<PagedResult<AuditLogDto>>.SuccessResponse(result));
    }
}
