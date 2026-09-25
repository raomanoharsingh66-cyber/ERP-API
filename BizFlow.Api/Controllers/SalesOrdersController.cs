using BizFlow.Api.Attributes;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Sales;
using BizFlow.Domain.Constants;
using BizFlow.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace BizFlow.Api.Controllers;

[Route("api/[controller]")]
[Route("api/sales-orders")]
public class SalesOrdersController : BaseApiController
{
    private readonly ISalesService _salesService;

    public SalesOrdersController(ISalesService salesService)
    {
        _salesService = salesService;
    }

    [HttpGet]
    [HasPermission(Permissions.Sales.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<SalesOrderDto>>>> GetOrders(
        [FromQuery] Guid? customerId,
        [FromQuery] OrderStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _salesService.GetSalesOrdersAsync(customerId, status, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Sales.View)]
    public async Task<ActionResult<ApiResponse<SalesOrderDto>>> GetOrderById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _salesService.GetSalesOrderByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Sales.Create)]
    public async Task<ActionResult<ApiResponse<SalesOrderDto>>> CreateOrder(
        [FromBody] CreateSalesOrderDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _salesService.CreateSalesOrderAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetOrderById), new { id = result.Data!.Id }, result);
    }

    [HttpPatch("{id:guid}/status")]
    [HasPermission(Permissions.Sales.Update)]
    public async Task<ActionResult<ApiResponse<SalesOrderDto>>> UpdateStatus(
        Guid id,
        [FromBody] UpdateOrderStatusDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _salesService.UpdateOrderStatusAsync(id, dto, cancellationToken);
        return Ok(result);
    }
}
