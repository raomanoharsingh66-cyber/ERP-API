using BizFlow.Api.Attributes;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Purchases;
using BizFlow.Domain.Constants;
using BizFlow.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace BizFlow.Api.Controllers;

[Route("api/[controller]")]
[Route("api/purchase-orders")]
public class PurchaseOrdersController : BaseApiController
{
    private readonly IPurchaseService _purchaseService;

    public PurchaseOrdersController(IPurchaseService purchaseService)
    {
        _purchaseService = purchaseService;
    }

    [HttpGet]
    [HasPermission(Permissions.Purchases.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<PurchaseOrderDto>>>> GetOrders(
        [FromQuery] Guid? supplierId,
        [FromQuery] PurchaseOrderStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _purchaseService.GetPurchaseOrdersAsync(supplierId, status, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Purchases.View)]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> GetOrderById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _purchaseService.GetPurchaseOrderByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Purchases.Create)]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> CreateOrder(
        [FromBody] CreatePurchaseOrderDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _purchaseService.CreatePurchaseOrderAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetOrderById), new { id = result.Data!.Id }, result);
    }

    [HttpPatch("{id:guid}/status")]
    [HasPermission(Permissions.Purchases.Approve)]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> UpdateStatus(
        Guid id,
        [FromBody] UpdatePurchaseOrderStatusDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _purchaseService.UpdatePurchaseOrderStatusAsync(id, dto, cancellationToken);
        return Ok(result);
    }
}
