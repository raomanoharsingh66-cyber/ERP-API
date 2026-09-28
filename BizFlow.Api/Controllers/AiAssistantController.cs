using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BizFlow.Api.Attributes;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.AiAssistant;
using BizFlow.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace BizFlow.Api.Controllers;

[Route("api/[controller]")]
[Route("api/ai-assistant")]
public class AiAssistantController : BaseApiController
{
    private readonly IAiAssistantService _aiAssistantService;

    public AiAssistantController(IAiAssistantService aiAssistantService)
    {
        _aiAssistantService = aiAssistantService;
    }

    [HttpGet("dashboard")]
    [HasPermission(Permissions.Purchases.View)]
    public async Task<ActionResult<ApiResponse<SmartPurchaseDashboardDto>>> GetDashboard(CancellationToken cancellationToken)
    {
        var result = await _aiAssistantService.GetSmartPurchaseDashboardAsync(cancellationToken);
        return Ok(ApiResponse<SmartPurchaseDashboardDto>.SuccessResponse(result));
    }

    [HttpGet("reorders")]
    [HasPermission(Permissions.Purchases.View)]
    public async Task<ActionResult<ApiResponse<List<SmartReorderRecommendationDto>>>> GetReorders(CancellationToken cancellationToken)
    {
        var result = await _aiAssistantService.GetSmartReordersAsync(cancellationToken);
        return Ok(ApiResponse<List<SmartReorderRecommendationDto>>.SuccessResponse(result));
    }

    [HttpGet("demand-trends")]
    [HasPermission(Permissions.Purchases.View)]
    public async Task<ActionResult<ApiResponse<List<DemandTrendDto>>>> GetDemandTrends(CancellationToken cancellationToken)
    {
        var result = await _aiAssistantService.GetDemandTrendsAsync(cancellationToken);
        return Ok(ApiResponse<List<DemandTrendDto>>.SuccessResponse(result));
    }

    [HttpGet("suppliers")]
    [HasPermission(Permissions.Purchases.View)]
    public async Task<ActionResult<ApiResponse<List<SupplierPerformanceDto>>>> GetSupplierPerformances(CancellationToken cancellationToken)
    {
        var result = await _aiAssistantService.GetSupplierPerformancesAsync(cancellationToken);
        return Ok(ApiResponse<List<SupplierPerformanceDto>>.SuccessResponse(result));
    }

    [HttpPost("analyze-quotation")]
    [HasPermission(Permissions.Purchases.View)]
    public async Task<ActionResult<ApiResponse<QuotationAnalysisResultDto>>> AnalyzeQuotation(
        [FromBody] QuotationAnalysisRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _aiAssistantService.AnalyzeQuotationAsync(request, cancellationToken);
        return Ok(ApiResponse<QuotationAnalysisResultDto>.SuccessResponse(result));
    }

    [HttpPost("chat")]
    [HasPermission(Permissions.Purchases.View)]
    public async Task<ActionResult<ApiResponse<AiChatResponseDto>>> AskAssistant(
        [FromBody] AiChatRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _aiAssistantService.AskAssistantAsync(request, cancellationToken);
        return Ok(ApiResponse<AiChatResponseDto>.SuccessResponse(result));
    }
}
