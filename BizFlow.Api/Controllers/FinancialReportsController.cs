using BizFlow.Api.Attributes;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Accounting;
using BizFlow.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace BizFlow.Api.Controllers;

[Route("api/reports")]
public class FinancialReportsController : BaseApiController
{
    private readonly IAccountingService _accountingService;

    public FinancialReportsController(IAccountingService accountingService)
    {
        _accountingService = accountingService;
    }

    [HttpGet("trial-balance")]
    [HasPermission(Permissions.Accounting.Reports)]
    public async Task<ActionResult<ApiResponse<TrialBalanceDto>>> GetTrialBalance(
        [FromQuery] DateTime? asOfDate,
        CancellationToken cancellationToken)
    {
        var result = await _accountingService.GetTrialBalanceAsync(asOfDate, cancellationToken);
        return Ok(ApiResponse<TrialBalanceDto>.SuccessResponse(result));
    }

    [HttpGet("profit-loss")]
    [HasPermission(Permissions.Accounting.Reports)]
    public async Task<ActionResult<ApiResponse<ProfitLossReportDto>>> GetProfitLoss(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var result = await _accountingService.GetProfitLossAsync(fromDate, toDate, cancellationToken);
        return Ok(ApiResponse<ProfitLossReportDto>.SuccessResponse(result));
    }

    [HttpGet("balance-sheet")]
    [HasPermission(Permissions.Accounting.Reports)]
    public async Task<ActionResult<ApiResponse<BalanceSheetReportDto>>> GetBalanceSheet(
        [FromQuery] DateTime? asOfDate,
        CancellationToken cancellationToken)
    {
        var result = await _accountingService.GetBalanceSheetAsync(asOfDate, cancellationToken);
        return Ok(ApiResponse<BalanceSheetReportDto>.SuccessResponse(result));
    }

    [HttpGet("gst-summary")]
    [HasPermission(Permissions.Accounting.Reports)]
    public async Task<ActionResult<ApiResponse<GstSummaryReportDto>>> GetGstSummary(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var result = await _accountingService.GetGstSummaryAsync(fromDate, toDate, cancellationToken);
        return Ok(ApiResponse<GstSummaryReportDto>.SuccessResponse(result));
    }
}
