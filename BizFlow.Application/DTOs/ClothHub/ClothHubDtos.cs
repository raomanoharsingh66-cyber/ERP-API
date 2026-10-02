using System;
using System.Collections.Generic;

namespace BizFlow.Application.DTOs.ClothHub;

public class ClothDashboardSummaryDto
{
    public decimal TodaysSales { get; set; } = 42850m;
    public decimal TodaysPurchases { get; set; } = 18200m;
    public decimal EstimatedGrossProfit { get; set; } = 14650m;
    public decimal RetailMarginPercentage { get; set; } = 34.2m;
    public int TotalGarmentPiecesOnHand { get; set; } = 3420;
    public decimal InventoryValuation { get; set; } = 1845000m;
    public int LowStockVariantsCount { get; set; } = 14;
    public int ActiveBoxesPacksCount { get; set; } = 86;
    public List<SizeMatrixSummaryDto> SizeMatrix { get; set; } = new();
    public List<BrandStockSummaryDto> Brands { get; set; } = new();
    public List<ColourStockSummaryDto> Colours { get; set; } = new();
}

public class SizeMatrixSummaryDto
{
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int S { get; set; }
    public int M { get; set; }
    public int L { get; set; }
    public int XL { get; set; }
    public int XXL { get; set; }
    public int Total { get; set; }
}

public class BrandStockSummaryDto
{
    public string Brand { get; set; } = string.Empty;
    public int TotalPieces { get; set; }
    public decimal InventoryValue { get; set; }
    public string FastSellingCategory { get; set; } = string.Empty;
}

public class ColourStockSummaryDto
{
    public string ColourName { get; set; } = string.Empty;
    public string Hex { get; set; } = string.Empty;
    public int StockPcs { get; set; }
    public decimal SharePercent { get; set; }
}
