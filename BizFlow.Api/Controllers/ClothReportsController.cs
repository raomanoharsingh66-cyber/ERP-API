using System;
using System.Threading;
using System.Threading.Tasks;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.ClothHub;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BizFlow.Api.Controllers;

[Authorize]
[Route("api/clothhub/reports")]
public class ClothReportsController : BaseApiController
{
    private readonly IClothAnalyticsService _analyticsService;

    public ClothReportsController(IClothAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    // 1. DAILY / MONTHLY SALES REPORT
    [HttpGet("daily-sales")]
    public async Task<ActionResult<ApiResponse<ClothDailySalesReportDto>>> GetDailySales(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetDailySalesReportAsync(fromDate, toDate, cancellationToken);
        return Ok(ApiResponse<ClothDailySalesReportDto>.SuccessResponse(result));
    }

    // 2. PROFIT & MARGIN REPORT
    [HttpGet("profit-margin")]
    public async Task<ActionResult<ApiResponse<ClothProfitMarginReportDto>>> GetProfitMargin(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetProfitAndMarginReportAsync(fromDate, toDate, cancellationToken);
        return Ok(ApiResponse<ClothProfitMarginReportDto>.SuccessResponse(result));
    }

    // 3. SIZE & COLOUR VELOCITY / PERFORMANCE
    [HttpGet("size-colour")]
    public async Task<ActionResult<ApiResponse<ClothSizeColourPerformanceReportDto>>> GetSizeColourPerformance(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetSizeColourPerformanceReportAsync(fromDate, toDate, cancellationToken);
        return Ok(ApiResponse<ClothSizeColourPerformanceReportDto>.SuccessResponse(result));
    }

    // 4. APPAREL GST REPORT (GSTR-1 SUMMARY)
    [HttpGet("gst-summary")]
    public async Task<ActionResult<ApiResponse<ClothApparelGstReportDto>>> GetGstReport(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetApparelGstReportAsync(fromDate, toDate, cancellationToken);
        return Ok(ApiResponse<ClothApparelGstReportDto>.SuccessResponse(result));
    }
}
