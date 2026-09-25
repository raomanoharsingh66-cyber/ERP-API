using BizFlow.Api.Attributes;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Sales;
using BizFlow.Domain.Constants;
using BizFlow.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace BizFlow.Api.Controllers;

[Route("api/[controller]")]
[Route("api/sales-invoices")]
public class SalesInvoicesController : BaseApiController
{
    private readonly ISalesService _salesService;

    public SalesInvoicesController(ISalesService salesService)
    {
        _salesService = salesService;
    }

    [HttpGet]
    [HasPermission(Permissions.Sales.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<SalesInvoiceDto>>>> GetInvoices(
        [FromQuery] Guid? customerId,
        [FromQuery] InvoiceStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _salesService.GetSalesInvoicesAsync(customerId, status, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("summary")]
    [HasPermission(Permissions.Sales.View)]
    public async Task<ActionResult<ApiResponse<SalesSummaryDto>>> GetSummary(CancellationToken cancellationToken)
    {
        var result = await _salesService.GetSalesSummaryAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Sales.View)]
    public async Task<ActionResult<ApiResponse<SalesInvoiceDto>>> GetInvoiceById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _salesService.GetSalesInvoiceByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Sales.Create)]
    public async Task<ActionResult<ApiResponse<SalesInvoiceDto>>> CreateInvoice(
        [FromBody] CreateSalesInvoiceDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _salesService.CreateSalesInvoiceAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetInvoiceById), new { id = result.Data!.Id }, result);
    }

    [HttpPost("{id:guid}/cancel")]
    [HasPermission(Permissions.Sales.Cancel)]
    public async Task<ActionResult<ApiResponse<bool>>> CancelInvoice(Guid id, CancellationToken cancellationToken)
    {
        var result = await _salesService.CancelSalesInvoiceAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("payments")]
    [HasPermission(Permissions.Sales.Create)]
    public async Task<ActionResult<ApiResponse<SalesPaymentDto>>> RecordPayment(
        [FromBody] RecordSalesPaymentDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _salesService.RecordPaymentAsync(dto, cancellationToken);
        return Ok(result);
    }
}
