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
[Route("api/clothhub/masters")]
public class ClothMastersController : BaseApiController
{
    private readonly IClothMasterService _masterService;

    public ClothMastersController(IClothMasterService masterService)
    {
        _masterService = masterService;
    }

    // BRANDS
    [HttpGet("brands")]
    public async Task<ActionResult<ApiResponse<List<ClothBrandDto>>>> GetBrands(CancellationToken cancellationToken)
    {
        var result = await _masterService.GetBrandsAsync(cancellationToken);
        return Ok(ApiResponse<List<ClothBrandDto>>.SuccessResponse(result));
    }

    [HttpPost("brands")]
    public async Task<ActionResult<ApiResponse<ClothBrandDto>>> CreateBrand([FromBody] CreateClothBrandDto dto, CancellationToken cancellationToken)
    {
        var result = await _masterService.CreateBrandAsync(dto, cancellationToken);
        return Ok(ApiResponse<ClothBrandDto>.SuccessResponse(result));
    }

    // CATEGORIES
    [HttpGet("categories")]
    public async Task<ActionResult<ApiResponse<List<ClothCategoryDto>>>> GetCategories(CancellationToken cancellationToken)
    {
        var result = await _masterService.GetCategoriesAsync(cancellationToken);
        return Ok(ApiResponse<List<ClothCategoryDto>>.SuccessResponse(result));
    }

    [HttpPost("categories")]
    public async Task<ActionResult<ApiResponse<ClothCategoryDto>>> CreateCategory([FromBody] CreateClothCategoryDto dto, CancellationToken cancellationToken)
    {
        var result = await _masterService.CreateCategoryAsync(dto, cancellationToken);
        return Ok(ApiResponse<ClothCategoryDto>.SuccessResponse(result));
    }

    // SIZES
    [HttpGet("sizes")]
    public async Task<ActionResult<ApiResponse<List<ClothSizeDto>>>> GetSizes(CancellationToken cancellationToken)
    {
        var result = await _masterService.GetSizesAsync(cancellationToken);
        return Ok(ApiResponse<List<ClothSizeDto>>.SuccessResponse(result));
    }

    [HttpPost("sizes")]
    public async Task<ActionResult<ApiResponse<ClothSizeDto>>> CreateSize([FromBody] CreateClothSizeDto dto, CancellationToken cancellationToken)
    {
        var result = await _masterService.CreateSizeAsync(dto, cancellationToken);
        return Ok(ApiResponse<ClothSizeDto>.SuccessResponse(result));
    }

    // COLOURS
    [HttpGet("colours")]
    public async Task<ActionResult<ApiResponse<List<ClothColourDto>>>> GetColours(CancellationToken cancellationToken)
    {
        var result = await _masterService.GetColoursAsync(cancellationToken);
        return Ok(ApiResponse<List<ClothColourDto>>.SuccessResponse(result));
    }

    [HttpPost("colours")]
    public async Task<ActionResult<ApiResponse<ClothColourDto>>> CreateColour([FromBody] CreateClothColourDto dto, CancellationToken cancellationToken)
    {
        var result = await _masterService.CreateColourAsync(dto, cancellationToken);
        return Ok(ApiResponse<ClothColourDto>.SuccessResponse(result));
    }

    // FABRICS
    [HttpGet("fabrics")]
    public async Task<ActionResult<ApiResponse<List<ClothFabricDto>>>> GetFabrics(CancellationToken cancellationToken)
    {
        var result = await _masterService.GetFabricsAsync(cancellationToken);
        return Ok(ApiResponse<List<ClothFabricDto>>.SuccessResponse(result));
    }

    [HttpPost("fabrics")]
    public async Task<ActionResult<ApiResponse<ClothFabricDto>>> CreateFabric([FromBody] CreateClothFabricDto dto, CancellationToken cancellationToken)
    {
        var result = await _masterService.CreateFabricAsync(dto, cancellationToken);
        return Ok(ApiResponse<ClothFabricDto>.SuccessResponse(result));
    }
}
