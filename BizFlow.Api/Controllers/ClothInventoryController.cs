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
[Route("api/clothhub/inventory")]
public class ClothInventoryController : BaseApiController
{
    private readonly IClothInventoryService _inventoryService;

    public ClothInventoryController(IClothInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    // 1. SIZE x COLOUR MATRIX
    [HttpGet("matrix/{productId:guid}")]
    public async Task<ActionResult<ApiResponse<ClothStockMatrixDto>>> GetStockMatrix(Guid productId, CancellationToken cancellationToken)
    {
        var matrix = await _inventoryService.GetStockMatrixAsync(productId, cancellationToken);
        if (matrix == null)
            return NotFound(ApiResponse<ClothStockMatrixDto>.FailureResponse("Product garment not found"));

        return Ok(ApiResponse<ClothStockMatrixDto>.SuccessResponse(matrix));
    }

    // 2. INVENTORY SUMMARY & LOW STOCK ALERTS
    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<ClothInventorySummaryDto>>> GetSummary(CancellationToken cancellationToken)
    {
        var summary = await _inventoryService.GetInventorySummaryAsync(cancellationToken);
        return Ok(ApiResponse<ClothInventorySummaryDto>.SuccessResponse(summary));
    }

    // 3. BOX & PACK ASSORTMENTS
    [HttpGet("box-packs")]
    public async Task<ActionResult<ApiResponse<List<ClothBoxPackDto>>>> GetBoxPacks(CancellationToken cancellationToken)
    {
        var packs = await _inventoryService.GetBoxPacksAsync(cancellationToken);
        return Ok(ApiResponse<List<ClothBoxPackDto>>.SuccessResponse(packs));
    }

    [HttpPost("box-packs")]
    public async Task<ActionResult<ApiResponse<ClothBoxPackDto>>> CreateBoxPack([FromBody] CreateClothBoxPackDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _inventoryService.CreateBoxPackAsync(dto, cancellationToken);
            return Ok(ApiResponse<ClothBoxPackDto>.SuccessResponse(result, "Box / Pack assortment registered successfully"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<ClothBoxPackDto>.FailureResponse(ex.Message));
        }
    }

    [HttpPost("box-packs/{id:guid}/unpack")]
    public async Task<ActionResult<ApiResponse<bool>>> UnpackBox(Guid id, [FromBody] UnpackBoxRequestDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _inventoryService.UnpackBoxAsync(id, dto.BoxesToUnpack, dto.Remarks, cancellationToken);
            return Ok(ApiResponse<bool>.SuccessResponse(success, $"Successfully unpacked {dto.BoxesToUnpack} box(es) into loose stock"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<bool>.FailureResponse(ex.Message));
        }
    }

    [HttpPost("box-packs/{id:guid}/pack")]
    public async Task<ActionResult<ApiResponse<bool>>> PackBox(Guid id, [FromBody] PackBoxRequestDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _inventoryService.PackBoxAsync(id, dto.BoxesToPack, dto.Remarks, cancellationToken);
            return Ok(ApiResponse<bool>.SuccessResponse(success, $"Successfully assembled {dto.BoxesToPack} box(es) from loose stock"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<bool>.FailureResponse(ex.Message));
        }
    }

    // 4. STOCK ADJUSTMENTS
    [HttpGet("adjustments")]
    public async Task<ActionResult<ApiResponse<List<ClothStockAdjustmentDto>>>> GetAdjustments(CancellationToken cancellationToken)
    {
        var adjustments = await _inventoryService.GetAdjustmentsAsync(cancellationToken);
        return Ok(ApiResponse<List<ClothStockAdjustmentDto>>.SuccessResponse(adjustments));
    }

    [HttpPost("adjustments")]
    public async Task<ActionResult<ApiResponse<ClothStockAdjustmentDto>>> CreateAdjustment([FromBody] CreateClothStockAdjustmentDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _inventoryService.CreateAdjustmentAsync(dto, cancellationToken);
            return Ok(ApiResponse<ClothStockAdjustmentDto>.SuccessResponse(result, "Physical stock adjustment verified and ledger posted"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<ClothStockAdjustmentDto>.FailureResponse(ex.Message));
        }
    }

    // 5. STOCK LEDGER AUDIT TRAIL
    [HttpGet("ledger")]
    public async Task<ActionResult<ApiResponse<List<ClothStockLedgerDto>>>> GetStockLedger(
        [FromQuery] Guid? variantId,
        [FromQuery] string? transactionType,
        CancellationToken cancellationToken)
    {
        var ledger = await _inventoryService.GetStockLedgerAsync(variantId, transactionType, cancellationToken);
        return Ok(ApiResponse<List<ClothStockLedgerDto>>.SuccessResponse(ledger));
    }
}
