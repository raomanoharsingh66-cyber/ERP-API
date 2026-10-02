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
[Route("api/clothhub/purchase")]
public class ClothPurchaseController : BaseApiController
{
    private readonly IClothPurchaseService _purchaseService;

    public ClothPurchaseController(IClothPurchaseService purchaseService)
    {
        _purchaseService = purchaseService;
    }

    // 1. SUPPLIERS
    [HttpGet("suppliers")]
    public async Task<ActionResult<ApiResponse<List<ClothSupplierDto>>>> GetSuppliers(CancellationToken cancellationToken)
    {
        var suppliers = await _purchaseService.GetSuppliersAsync(cancellationToken);
        return Ok(ApiResponse<List<ClothSupplierDto>>.SuccessResponse(suppliers));
    }

    [HttpPost("suppliers")]
    public async Task<ActionResult<ApiResponse<ClothSupplierDto>>> CreateSupplier([FromBody] CreateClothSupplierDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _purchaseService.CreateSupplierAsync(dto, cancellationToken);
            return Ok(ApiResponse<ClothSupplierDto>.SuccessResponse(result, "Apparel supplier registered successfully"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<ClothSupplierDto>.FailureResponse(ex.Message));
        }
    }

    // 2. PURCHASE BILLS & GOODS INWARD
    [HttpGet("bills")]
    public async Task<ActionResult<ApiResponse<List<ClothPurchaseBillDto>>>> GetBills(CancellationToken cancellationToken)
    {
        var bills = await _purchaseService.GetPurchaseBillsAsync(cancellationToken);
        return Ok(ApiResponse<List<ClothPurchaseBillDto>>.SuccessResponse(bills));
    }

    [HttpGet("bills/{id:guid}")]
    public async Task<ActionResult<ApiResponse<ClothPurchaseBillDto>>> GetBillById(Guid id, CancellationToken cancellationToken)
    {
        var bill = await _purchaseService.GetPurchaseBillByIdAsync(id, cancellationToken);
        if (bill == null)
            return NotFound(ApiResponse<ClothPurchaseBillDto>.FailureResponse("Purchase bill not found"));

        return Ok(ApiResponse<ClothPurchaseBillDto>.SuccessResponse(bill));
    }

    [HttpPost("bills")]
    public async Task<ActionResult<ApiResponse<ClothPurchaseBillDto>>> CreateBill([FromBody] CreateClothPurchaseBillDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _purchaseService.CreatePurchaseBillAsync(dto, cancellationToken);
            return Ok(ApiResponse<ClothPurchaseBillDto>.SuccessResponse(result, "Purchase bill generated and stock inward posted"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<ClothPurchaseBillDto>.FailureResponse(ex.Message));
        }
    }

    [HttpPost("bills/{id:guid}/pay")]
    public async Task<ActionResult<ApiResponse<bool>>> RecordPayment(Guid id, [FromBody] RecordSupplierPaymentDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _purchaseService.RecordSupplierPaymentAsync(id, dto, cancellationToken);
            return Ok(ApiResponse<bool>.SuccessResponse(result, "Supplier payment recorded and ledger updated"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<bool>.FailureResponse(ex.Message));
        }
    }

    // 3. PURCHASE RETURNS & DEBIT NOTES
    [HttpGet("returns")]
    public async Task<ActionResult<ApiResponse<List<ClothPurchaseReturnDto>>>> GetReturns(CancellationToken cancellationToken)
    {
        var returns = await _purchaseService.GetPurchaseReturnsAsync(cancellationToken);
        return Ok(ApiResponse<List<ClothPurchaseReturnDto>>.SuccessResponse(returns));
    }

    [HttpPost("returns")]
    public async Task<ActionResult<ApiResponse<ClothPurchaseReturnDto>>> CreateReturn([FromBody] CreateClothPurchaseReturnDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _purchaseService.CreatePurchaseReturnAsync(dto, cancellationToken);
            return Ok(ApiResponse<ClothPurchaseReturnDto>.SuccessResponse(result, "Purchase return processed and debit note issued"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<ClothPurchaseReturnDto>.FailureResponse(ex.Message));
        }
    }

    // 4. SUMMARY
    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<ClothPurchaseSummaryDto>>> GetSummary(CancellationToken cancellationToken)
    {
        var summary = await _purchaseService.GetPurchaseSummaryAsync(cancellationToken);
        return Ok(ApiResponse<ClothPurchaseSummaryDto>.SuccessResponse(summary));
    }
}
