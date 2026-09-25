using BizFlow.Api.Attributes;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Inventory;
using BizFlow.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace BizFlow.Api.Controllers;

public class InventoryController : BaseApiController
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet("products/{productId:guid}/stocks")]
    [HasPermission(Permissions.Inventory.View)]
    public async Task<ActionResult<ApiResponse<List<InventoryStockDto>>>> GetProductStocks(
        Guid productId, 
        CancellationToken cancellationToken)
    {
        var result = await _inventoryService.GetProductStocksAsync(productId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("warehouses/{warehouseId:guid}/stocks")]
    [HasPermission(Permissions.Inventory.View)]
    public async Task<ActionResult<ApiResponse<List<InventoryStockDto>>>> GetWarehouseStocks(
        Guid warehouseId, 
        CancellationToken cancellationToken)
    {
        var result = await _inventoryService.GetWarehouseStocksAsync(warehouseId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("adjust-stock")]
    [HasPermission(Permissions.Inventory.AdjustStock)]
    public async Task<ActionResult<ApiResponse<InventoryStockDto>>> AdjustStock(
        [FromBody] StockAdjustmentDto dto, 
        CancellationToken cancellationToken)
    {
        var result = await _inventoryService.AdjustStockAsync(dto, cancellationToken);
        return Ok(result);
    }

    [HttpPost("transfer-stock")]
    [HasPermission(Permissions.Inventory.AdjustStock)]
    public async Task<ActionResult<ApiResponse<bool>>> TransferStock(
        [FromBody] StockTransferDto dto, 
        CancellationToken cancellationToken)
    {
        var result = await _inventoryService.TransferStockAsync(dto, cancellationToken);
        return Ok(result);
    }

    [HttpGet("transactions")]
    [HasPermission(Permissions.Inventory.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<StockTransactionDto>>>> GetTransactions(
        [FromQuery] Guid? productId,
        [FromQuery] Guid? warehouseId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _inventoryService.GetStockTransactionsAsync(productId, warehouseId, pageNumber, pageSize, cancellationToken);
        return Ok(result);
    }
}
