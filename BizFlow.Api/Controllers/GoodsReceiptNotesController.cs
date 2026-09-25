using BizFlow.Api.Attributes;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Purchases;
using BizFlow.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace BizFlow.Api.Controllers;

[Route("api/[controller]")]
[Route("api/goods-receipt-notes")]
public class GoodsReceiptNotesController : BaseApiController
{
    private readonly IPurchaseService _purchaseService;

    public GoodsReceiptNotesController(IPurchaseService purchaseService)
    {
        _purchaseService = purchaseService;
    }

    [HttpGet]
    [HasPermission(Permissions.Purchases.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<GoodsReceiptNoteDto>>>> GetGoodsReceipts(
        [FromQuery] Guid? purchaseOrderId,
        [FromQuery] Guid? supplierId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _purchaseService.GetGoodsReceiptNotesAsync(purchaseOrderId, supplierId, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Purchases.View)]
    public async Task<ActionResult<ApiResponse<GoodsReceiptNoteDto>>> GetGoodsReceiptById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _purchaseService.GetGoodsReceiptNoteByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Purchases.Create)]
    public async Task<ActionResult<ApiResponse<GoodsReceiptNoteDto>>> CreateGoodsReceipt(
        [FromBody] CreateGoodsReceiptNoteDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _purchaseService.CreateGoodsReceiptNoteAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetGoodsReceiptById), new { id = result.Data!.Id }, result);
    }
}
