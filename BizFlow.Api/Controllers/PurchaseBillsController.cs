using BizFlow.Api.Attributes;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Purchases;
using BizFlow.Domain.Constants;
using BizFlow.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace BizFlow.Api.Controllers;

[Route("api/[controller]")]
[Route("api/purchase-bills")]
public class PurchaseBillsController : BaseApiController
{
    private readonly IPurchaseService _purchaseService;

    public PurchaseBillsController(IPurchaseService purchaseService)
    {
        _purchaseService = purchaseService;
    }

    [HttpGet]
    [HasPermission(Permissions.Purchases.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<PurchaseBillDto>>>> GetBills(
        [FromQuery] Guid? supplierId,
        [FromQuery] BillStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _purchaseService.GetPurchaseBillsAsync(supplierId, status, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("summary")]
    [HasPermission(Permissions.Purchases.View)]
    public async Task<ActionResult<ApiResponse<PurchaseSummaryDto>>> GetSummary(CancellationToken cancellationToken)
    {
        var result = await _purchaseService.GetPurchaseSummaryAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Purchases.View)]
    public async Task<ActionResult<ApiResponse<PurchaseBillDto>>> GetBillById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _purchaseService.GetPurchaseBillByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Purchases.Create)]
    public async Task<ActionResult<ApiResponse<PurchaseBillDto>>> CreateBill(
        [FromBody] CreatePurchaseBillDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _purchaseService.CreatePurchaseBillAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetBillById), new { id = result.Data!.Id }, result);
    }

    [HttpPost("{id:guid}/cancel")]
    [HasPermission(Permissions.Purchases.Approve)]
    public async Task<ActionResult<ApiResponse<bool>>> CancelBill(Guid id, CancellationToken cancellationToken)
    {
        var result = await _purchaseService.CancelPurchaseBillAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("payments")]
    [HasPermission(Permissions.Purchases.Approve)]
    public async Task<ActionResult<ApiResponse<VendorPaymentDto>>> RecordPayment(
        [FromBody] RecordVendorPaymentDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _purchaseService.RecordVendorPaymentAsync(dto, cancellationToken);
        return Ok(result);
    }
}
