using System;
using System.Collections.Generic;

namespace BizFlow.Application.DTOs.ClothHub;

// --- SIZE x COLOUR STOCK MATRIX DTOs ---

public class ClothStockMatrixDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SkuPrefix { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal BaseMrp { get; set; }
    public decimal BaseSellingPrice { get; set; }
    public int TotalStock { get; set; }
    public decimal TotalValuation { get; set; }

    public List<MatrixColumnSizeDto> Sizes { get; set; } = new();
    public List<MatrixRowColourDto> Rows { get; set; } = new();
}

public class MatrixColumnSizeDto
{
    public Guid SizeId { get; set; }
    public string SizeName { get; set; } = string.Empty;
    public string SizeCode { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public int TotalSizeStock { get; set; }
}

public class MatrixRowColourDto
{
    public Guid ColourId { get; set; }
    public string ColourName { get; set; } = string.Empty;
    public string ColourHex { get; set; } = string.Empty;
    public int TotalColourStock { get; set; }
    public List<MatrixCellVariantDto> Cells { get; set; } = new();
}

public class MatrixCellVariantDto
{
    public Guid VariantId { get; set; }
    public Guid SizeId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int MinStockLevel { get; set; }
    public decimal Mrp { get; set; }
    public decimal SellingPrice { get; set; }
    public bool IsLowStock => CurrentStock <= MinStockLevel;
}

// --- BOX & PACK ASSORTMENT DTOs ---

public class ClothBoxPackDto
{
    public Guid Id { get; set; }
    public Guid ClothProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string PackName { get; set; } = string.Empty;
    public string PackCode { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public int PiecesPerPack { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal Mrp { get; set; }
    public int CurrentBoxStock { get; set; }
    public int TotalGarmentPiecesEquivalent => CurrentBoxStock * PiecesPerPack;
    public string? AssortmentDescription { get; set; }
    public bool IsActive { get; set; }
    public List<ClothBoxPackItemDto> Items { get; set; } = new();
}

public class ClothBoxPackItemDto
{
    public Guid Id { get; set; }
    public Guid ClothProductVariantId { get; set; }
    public string VariantSku { get; set; } = string.Empty;
    public string SizeName { get; set; } = string.Empty;
    public string ColourName { get; set; } = string.Empty;
    public int QuantityPerBox { get; set; }
}

public class CreateClothBoxPackDto
{
    public Guid ClothProductId { get; set; }
    public string PackName { get; set; } = string.Empty;
    public string PackCode { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public int PiecesPerPack { get; set; } = 12;
    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal Mrp { get; set; }
    public int InitialBoxStock { get; set; } = 0;
    public string? AssortmentDescription { get; set; }
    public List<CreateClothBoxPackItemDto> Items { get; set; } = new();
}

public class CreateClothBoxPackItemDto
{
    public Guid ClothProductVariantId { get; set; }
    public int QuantityPerBox { get; set; } = 1;
}

public class UnpackBoxRequestDto
{
    public int BoxesToUnpack { get; set; } = 1;
    public string? Remarks { get; set; }
}

public class PackBoxRequestDto
{
    public int BoxesToPack { get; set; } = 1;
    public string? Remarks { get; set; }
}

// --- STOCK ADJUSTMENT DTOs ---

public class ClothStockAdjustmentDto
{
    public Guid Id { get; set; }
    public string AdjustmentNumber { get; set; } = string.Empty;
    public DateTime AdjustmentDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int TotalItemsCount { get; set; }
    public int TotalNetQuantityDiff { get; set; }
    public decimal TotalValueImpact { get; set; }
    public string? Remarks { get; set; }
    public List<ClothStockAdjustmentItemDto> Items { get; set; } = new();
}

public class ClothStockAdjustmentItemDto
{
    public Guid Id { get; set; }
    public Guid ClothProductVariantId { get; set; }
    public string VariantSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string SizeName { get; set; } = string.Empty;
    public string ColourName { get; set; } = string.Empty;
    public int PreviousStock { get; set; }
    public int AdjustedStock { get; set; }
    public int Difference { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalValueImpact { get; set; }
}

public class CreateClothStockAdjustmentDto
{
    public string Reason { get; set; } = "Physical Count Reconcile";
    public string? Remarks { get; set; }
    public List<CreateClothStockAdjustmentItemDto> Items { get; set; } = new();
}

public class CreateClothStockAdjustmentItemDto
{
    public Guid ClothProductVariantId { get; set; }
    public int AdjustedStock { get; set; } // The actual verified count
}

// --- STOCK LEDGER DTOs ---

public class ClothStockLedgerDto
{
    public Guid Id { get; set; }
    public Guid ClothProductVariantId { get; set; }
    public string VariantSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string SizeName { get; set; } = string.Empty;
    public string ColourName { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public int QuantityIn { get; set; }
    public int QuantityOut { get; set; }
    public int RunningBalance { get; set; }
    public decimal UnitCost { get; set; }
    public string? Notes { get; set; }
}

// --- OVERALL SUMMARY DTO ---

public class ClothInventorySummaryDto
{
    public int TotalProducts { get; set; }
    public int TotalVariants { get; set; }
    public int TotalGarmentPieces { get; set; }
    public int TotalBoxPacks { get; set; }
    public decimal TotalInventoryValuation { get; set; }
    public int LowStockItemsCount { get; set; }
    public List<BrandStockShareDto> BrandBreakdown { get; set; } = new();
    public List<SizeStockShareDto> SizeBreakdown { get; set; } = new();
    public List<LowStockAlertDto> LowStockAlerts { get; set; } = new();
}

public class BrandStockShareDto
{
    public string BrandName { get; set; } = string.Empty;
    public int TotalPieces { get; set; }
    public decimal TotalValuation { get; set; }
}

public class SizeStockShareDto
{
    public string SizeName { get; set; } = string.Empty;
    public int TotalPieces { get; set; }
}

public class LowStockAlertDto
{
    public Guid VariantId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string SizeName { get; set; } = string.Empty;
    public string ColourName { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int MinStockLevel { get; set; }
}
