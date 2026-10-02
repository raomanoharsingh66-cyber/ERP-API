using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.ClothHub;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BizFlow.Api.Controllers;

[Authorize]
[Route("api/clothhub/expenses")]
public class ClothExpensesController : BaseApiController
{
    private readonly IClothExpenseService _expenseService;

    public ClothExpensesController(IClothExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ClothExpenseDto>>>> GetExpenses(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? category,
        CancellationToken cancellationToken)
    {
        var result = await _expenseService.GetExpensesAsync(fromDate, toDate, category, cancellationToken);
        return Ok(ApiResponse<List<ClothExpenseDto>>.SuccessResponse(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ClothExpenseDto>>> CreateExpense(
        [FromBody] CreateClothExpenseDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _expenseService.CreateExpenseAsync(dto, cancellationToken);
            return Ok(ApiResponse<ClothExpenseDto>.SuccessResponse(result, "Store expense voucher recorded successfully"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<ClothExpenseDto>.FailureResponse(ex.Message));
        }
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<ClothExpenseSummaryDto>>> GetSummary(CancellationToken cancellationToken)
    {
        var result = await _expenseService.GetExpenseSummaryAsync(cancellationToken);
        return Ok(ApiResponse<ClothExpenseSummaryDto>.SuccessResponse(result));
    }
}
