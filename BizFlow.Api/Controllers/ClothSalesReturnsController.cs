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
[Route("api/clothhub/sales/returns")]
public class ClothSalesReturnsController : BaseApiController
{
    private readonly IClothSalesReturnService _returnService;

    public ClothSalesReturnsController(IClothSalesReturnService returnService)
    {
        _returnService = returnService;
    }

    // 1. GET ALL SALES RETURNS / EXCHANGES
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ClothSalesReturnDto>>>> GetReturns(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? search,
        [FromQuery] string? returnType,
        CancellationToken cancellationToken)
    {
        var returns = await _returnService.GetReturnsAsync(fromDate, toDate, search, returnType, cancellationToken);
        return Ok(ApiResponse<List<ClothSalesReturnDto>>.SuccessResponse(returns));
    }

    // 2. GET RETURN / EXCHANGE BY ID
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ClothSalesReturnDto>>> GetReturnById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _returnService.GetReturnByIdAsync(id, cancellationToken);
        if (result == null)
            return NotFound(ApiResponse<ClothSalesReturnDto>.FailureResponse("Return/Exchange transaction not found"));

        return Ok(ApiResponse<ClothSalesReturnDto>.SuccessResponse(result));
    }

    // 3. PROCESS RETURN OR EXCHANGE
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ClothSalesReturnDto>>> CreateReturnOrExchange(
        [FromBody] CreateClothSalesReturnDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _returnService.CreateReturnOrExchangeAsync(dto, cancellationToken);
            var actionText = string.Equals(dto.ReturnType, "Exchange", StringComparison.OrdinalIgnoreCase)
                ? "Garment exchange processed successfully"
                : "Garment return processed successfully";
            return Ok(ApiResponse<ClothSalesReturnDto>.SuccessResponse(result, actionText));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<ClothSalesReturnDto>.FailureResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<ClothSalesReturnDto>.FailureResponse("Failed to process transaction: " + ex.Message));
        }
    }

    // 4. RETURNS & EXCHANGES SUMMARY KPI
    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<ClothSalesReturnSummaryDto>>> GetSummary(CancellationToken cancellationToken)
    {
        var summary = await _returnService.GetReturnsSummaryAsync(cancellationToken);
        return Ok(ApiResponse<ClothSalesReturnSummaryDto>.SuccessResponse(summary));
    }
}
