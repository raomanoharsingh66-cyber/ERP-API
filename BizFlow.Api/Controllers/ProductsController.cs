using BizFlow.Api.Attributes;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Inventory;
using BizFlow.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace BizFlow.Api.Controllers;

public class ProductsController : BaseApiController
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    [HasPermission(Permissions.Inventory.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductListItemDto>>>> GetProducts(
        [FromQuery] ProductFilterDto filter, 
        CancellationToken cancellationToken)
    {
        var result = await _productService.GetProductsAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Inventory.View)]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetProductById(
        Guid id, 
        CancellationToken cancellationToken)
    {
        var result = await _productService.GetProductByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("sku/{sku}")]
    [HasPermission(Permissions.Inventory.View)]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetProductBySku(
        string sku, 
        CancellationToken cancellationToken)
    {
        var result = await _productService.GetProductBySkuAsync(sku, cancellationToken);
        return Ok(result);
    }

    [HttpGet("low-stock")]
    [HasPermission(Permissions.Inventory.View)]
    public async Task<ActionResult<ApiResponse<List<ProductListItemDto>>>> GetLowStockProducts(CancellationToken cancellationToken)
    {
        var result = await _productService.GetLowStockProductsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Inventory.Create)]
    public async Task<ActionResult<ApiResponse<ProductDto>>> CreateProduct(
        [FromBody] CreateProductDto dto, 
        CancellationToken cancellationToken)
    {
        var result = await _productService.CreateProductAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetProductById), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Inventory.Update)]
    public async Task<ActionResult<ApiResponse<ProductDto>>> UpdateProduct(
        Guid id, 
        [FromBody] UpdateProductDto dto, 
        CancellationToken cancellationToken)
    {
        var result = await _productService.UpdateProductAsync(id, dto, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Inventory.Delete)]
    public async Task<ActionResult<ApiResponse>> DeleteProduct(
        Guid id, 
        CancellationToken cancellationToken)
    {
        var result = await _productService.DeleteProductAsync(id, cancellationToken);
        return Ok(result);
    }
}
