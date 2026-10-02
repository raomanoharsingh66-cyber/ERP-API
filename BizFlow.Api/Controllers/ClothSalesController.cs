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
[Route("api/clothhub/sales")]
public class ClothSalesController : BaseApiController
{
    private readonly IClothSalesService _salesService;

    public ClothSalesController(IClothSalesService salesService)
    {
        _salesService = salesService;
    }

    // 1. FAST BARCODE / SKU LOOKUP FOR SCANNER
    [HttpGet("lookup")]
    public async Task<ActionResult<ApiResponse<ClothPosLookupItemDto?>>> LookupVariant([FromQuery] string term, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(term))
            return BadRequest(ApiResponse<ClothPosLookupItemDto?>.FailureResponse("Lookup barcode or SKU term is required"));

        var item = await _salesService.LookupVariantByBarcodeOrSkuAsync(term, cancellationToken);
        if (item == null)
            return NotFound(ApiResponse<ClothPosLookupItemDto?>.FailureResponse("Garment item not found for barcode/SKU: " + term));

        return Ok(ApiResponse<ClothPosLookupItemDto?>.SuccessResponse(item));
    }

    // 2. POS QUICK ITEM SEARCH / CATALOGUE
    [HttpGet("pos-items")]
    public async Task<ActionResult<ApiResponse<List<ClothPosLookupItemDto>>>> SearchPosItems(
        [FromQuery] string? query, 
        [FromQuery] Guid? categoryId, 
        CancellationToken cancellationToken)
    {
        var items = await _salesService.SearchPosItemsAsync(query, categoryId, cancellationToken);
        return Ok(ApiResponse<List<ClothPosLookupItemDto>>.SuccessResponse(items));
    }

    // 3. SALES INVOICES LIST
    [HttpGet("invoices")]
    public async Task<ActionResult<ApiResponse<List<ClothSalesInvoiceDto>>>> GetInvoices(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var invoices = await _salesService.GetSalesInvoicesAsync(fromDate, toDate, search, cancellationToken);
        return Ok(ApiResponse<List<ClothSalesInvoiceDto>>.SuccessResponse(invoices));
    }

    // 4. SALES INVOICE BY ID (PRINT / VIEW)
    [HttpGet("invoices/{id:guid}")]
    public async Task<ActionResult<ApiResponse<ClothSalesInvoiceDto>>> GetInvoiceById(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await _salesService.GetSalesInvoiceByIdAsync(id, cancellationToken);
        if (invoice == null)
            return NotFound(ApiResponse<ClothSalesInvoiceDto>.FailureResponse("Sales invoice not found"));

        return Ok(ApiResponse<ClothSalesInvoiceDto>.SuccessResponse(invoice));
    }

    // 5. CREATE POS BILL / INVOICE
    [HttpPost("invoices")]
    public async Task<ActionResult<ApiResponse<ClothSalesInvoiceDto>>> CreateInvoice([FromBody] CreateClothSalesInvoiceDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var invoice = await _salesService.CreateSalesInvoiceAsync(dto, cancellationToken);
            return Ok(ApiResponse<ClothSalesInvoiceDto>.SuccessResponse(invoice, "Invoice generated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<ClothSalesInvoiceDto>.FailureResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<ClothSalesInvoiceDto>.FailureResponse("Failed to complete billing: " + ex.Message));
        }
    }

    // 6. SALES KPI & SUMMARY
    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<ClothSalesSummaryDto>>> GetSummary(CancellationToken cancellationToken)
    {
        var summary = await _salesService.GetSalesSummaryAsync(cancellationToken);
        return Ok(ApiResponse<ClothSalesSummaryDto>.SuccessResponse(summary));
    }
}
