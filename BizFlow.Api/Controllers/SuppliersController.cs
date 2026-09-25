using BizFlow.Api.Attributes;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Purchases;
using BizFlow.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace BizFlow.Api.Controllers;

public class SuppliersController : BaseApiController
{
    private readonly ISupplierService _supplierService;

    public SuppliersController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    [HttpGet]
    [HasPermission(Permissions.Suppliers.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<SupplierDto>>>> GetSuppliers(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _supplierService.GetSuppliersAsync(search, isActive, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Suppliers.View)]
    public async Task<ActionResult<ApiResponse<SupplierDto>>> GetSupplierById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _supplierService.GetSupplierByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Suppliers.Create)]
    public async Task<ActionResult<ApiResponse<SupplierDto>>> CreateSupplier(
        [FromBody] CreateSupplierDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _supplierService.CreateSupplierAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetSupplierById), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Suppliers.Update)]
    public async Task<ActionResult<ApiResponse<SupplierDto>>> UpdateSupplier(
        Guid id,
        [FromBody] UpdateSupplierDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _supplierService.UpdateSupplierAsync(id, dto, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Suppliers.Delete)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteSupplier(Guid id, CancellationToken cancellationToken)
    {
        var result = await _supplierService.DeleteSupplierAsync(id, cancellationToken);
        return Ok(result);
    }
}
