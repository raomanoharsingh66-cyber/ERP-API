using BizFlow.Api.Attributes;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Dashboard;
using Microsoft.AspNetCore.Mvc;

namespace BizFlow.Api.Controllers;

public class DashboardController : BaseApiController
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<DashboardMetricsDto>>> GetDashboardStats(CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetExecutiveDashboardAsync(cancellationToken);
        return Ok(ApiResponse<DashboardMetricsDto>.SuccessResponse(result));
    }
}
