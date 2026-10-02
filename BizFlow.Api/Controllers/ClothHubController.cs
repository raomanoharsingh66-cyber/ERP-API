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
[Route("api/clothhub")]
public class ClothHubController : BaseApiController
{
    private readonly ICurrentUserService _currentUserService;

    public ClothHubController(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    [HttpGet("health")]
    public ActionResult<ApiResponse<object>> GetClothHubHealth()
    {
        return Ok(ApiResponse<object>.SuccessResponse(new
        {
            Module = "Cloth Hub",
            Status = "Online",
            Version = "Phase 1 - Retail System Structure",
            TenantId = _currentUserService.BusinessId,
            User = _currentUserService.Email,
            Timestamp = DateTime.UtcNow
        }));
    }

    [HttpGet("dashboard")]
    public ActionResult<ApiResponse<ClothDashboardSummaryDto>> GetDashboardSummary(CancellationToken cancellationToken)
    {
        var summary = new ClothDashboardSummaryDto
        {
            TodaysSales = 42850m,
            TodaysPurchases = 18200m,
            EstimatedGrossProfit = 14650m,
            RetailMarginPercentage = 34.2m,
            TotalGarmentPiecesOnHand = 3420,
            InventoryValuation = 1845000m,
            LowStockVariantsCount = 14,
            ActiveBoxesPacksCount = 86,
            SizeMatrix = new List<SizeMatrixSummaryDto>
            {
                new() { ProductName = "Men's Cotton Formal Shirt", Category = "Formal Shirts", S = 8, M = 18, L = 22, XL = 14, XXL = 6, Total = 68 },
                new() { ProductName = "Slim Fit Stretch Denim Jeans", Category = "Denim & Jeans", S = 4, M = 15, L = 19, XL = 12, XXL = 4, Total = 54 },
                new() { ProductName = "Women's Anarkali Embroidered Kurti", Category = "Ethnic Kurtis", S = 12, M = 24, L = 20, XL = 10, XXL = 2, Total = 68 },
                new() { ProductName = "Casual Round Neck Cotton T-Shirt", Category = "Casual T-Shirts", S = 15, M = 35, L = 40, XL = 25, XXL = 12, Total = 127 },
                new() { ProductName = "Kanjivaram Silk Zari Saree", Category = "Ethnic Sarees", S = 0, M = 0, L = 0, XL = 0, XXL = 0, Total = 45 }
            },
            Brands = new List<BrandStockSummaryDto>
            {
                new() { Brand = "Allen Solly", TotalPieces = 640, InventoryValue = 480000m, FastSellingCategory = "Casual Shirts" },
                new() { Brand = "Manyavar Ethnic", TotalPieces = 420, InventoryValue = 560000m, FastSellingCategory = "Kurta Sets" },
                new() { Brand = "Raymond Fine", TotalPieces = 510, InventoryValue = 390000m, FastSellingCategory = "Trouser Fabrics" },
                new() { Brand = "Levi's Denim", TotalPieces = 580, InventoryValue = 415000m, FastSellingCategory = "Stretch Jeans" }
            },
            Colours = new List<ColourStockSummaryDto>
            {
                new() { ColourName = "Jet Black", Hex = "#111827", StockPcs = 840, SharePercent = 28m },
                new() { ColourName = "Navy Blue", Hex = "#1e3a8a", StockPcs = 690, SharePercent = 23m },
                new() { ColourName = "Crisp White", Hex = "#f8fafc", StockPcs = 580, SharePercent = 19m },
                new() { ColourName = "Olive Green", Hex = "#4d7c0f", StockPcs = 420, SharePercent = 14m },
                new() { ColourName = "Deep Maroon", Hex = "#881337", StockPcs = 360, SharePercent = 12m }
            }
        };

        return Ok(ApiResponse<ClothDashboardSummaryDto>.SuccessResponse(summary));
    }
}
