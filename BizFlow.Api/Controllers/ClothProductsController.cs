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
[Route("api/clothhub/products")]
public class ClothProductsController : BaseApiController
{
    private readonly IClothProductService _productService;

    public ClothProductsController(IClothProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ClothProductDetailDto>>>> GetProducts(
        [FromQuery] string? search,
        [FromQuery] Guid? brandId,
        [FromQuery] Guid? categoryId,
        CancellationToken cancellationToken)
    {
        var result = await _productService.GetProductsAsync(search, brandId, categoryId, cancellationToken);
        return Ok(ApiResponse<List<ClothProductDetailDto>>.SuccessResponse(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ClothProductDetailDto>>> GetProductById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _productService.GetProductByIdAsync(id, cancellationToken);
        if (result == null)
        {
            return NotFound(ApiResponse<ClothProductDetailDto>.FailureResponse("Product not found", "No garment product found with the specified ID."));
        }
        return Ok(ApiResponse<ClothProductDetailDto>.SuccessResponse(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ClothProductDetailDto>>> CreateProductWithVariants(
        [FromBody] CreateClothProductRequestDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _productService.CreateProductWithVariantsAsync(dto, cancellationToken);
        return Ok(ApiResponse<ClothProductDetailDto>.SuccessResponse(result, "Garment product and size-colour variant matrix generated successfully."));
    }

    [HttpPut("variants/{variantId:guid}/stock")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateVariantStock(
        Guid variantId,
        [FromBody] int newStock,
        CancellationToken cancellationToken)
    {
        var result = await _productService.UpdateVariantStockAsync(variantId, newStock, cancellationToken);
        return Ok(ApiResponse<bool>.SuccessResponse(result, "Variant stock updated."));
    }
}
