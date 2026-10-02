using System;
using System.Collections.Generic;
using BizFlow.Domain.Common;

namespace BizFlow.Domain.Entities.ClothHub;

public class ClothBoxPack : BaseEntity, IBusinessScoped
{
    public Guid BusinessId { get; set; }
    
    public Guid ClothProductId { get; set; }
    public ClothProduct? Product { get; set; }

    public string PackName { get; set; } = string.Empty; // e.g. "Pack of 12 - Men's Polo Multi-Assorted"
    public string PackCode { get; set; } = string.Empty; // e.g. "BOX-POLO-12"
    public string Barcode { get; set; } = string.Empty; // Pack outer barcode

    public int PiecesPerPack { get; set; } = 12; // Total loose garments inside 1 box
    public decimal CostPrice { get; set; } // Purchase cost of 1 box
    public decimal SellingPrice { get; set; } // Wholesale or bundle selling price
    public decimal Mrp { get; set; } // Printed box MRP

    public int CurrentBoxStock { get; set; } = 0; // Number of sealed boxes on shelf
    public string? AssortmentDescription { get; set; } // e.g. "2xS, 3xM, 4xL, 3xXL (Assorted Colours)"
    public bool IsActive { get; set; } = true;

    // Breakdown of specific variants in this box assortment
    public ICollection<ClothBoxPackItem> Items { get; set; } = new List<ClothBoxPackItem>();
}

public class ClothBoxPackItem : BaseEntity
{
    public Guid ClothBoxPackId { get; set; }
    public ClothBoxPack? BoxPack { get; set; }

    public Guid ClothProductVariantId { get; set; }
    public ClothProductVariant? Variant { get; set; }

    public int QuantityPerBox { get; set; } = 1; // Number of this specific size-colour variant per box
}

public class ClothStockAdjustment : BaseEntity, IBusinessScoped
{
    public Guid BusinessId { get; set; }
    
    public string AdjustmentNumber { get; set; } = string.Empty; // e.g. "ADJ-2026-0001"
    public DateTime AdjustmentDate { get; set; } = DateTime.UtcNow;
    
    // Reason: "Physical Count Reconcile", "Damaged Garment", "Theft or Shrinkage", "Sample / Display Piece", "Return to Vendor"
    public string Reason { get; set; } = "Physical Count Reconcile";
    
    public int TotalItemsCount { get; set; } = 0;
    public int TotalNetQuantityDiff { get; set; } = 0; // Net sum of +/- pieces
    public decimal TotalValueImpact { get; set; } = 0;
    public string? Remarks { get; set; }

    public ICollection<ClothStockAdjustmentItem> Items { get; set; } = new List<ClothStockAdjustmentItem>();
}

public class ClothStockAdjustmentItem : BaseEntity
{
    public Guid ClothStockAdjustmentId { get; set; }
    public ClothStockAdjustment? Adjustment { get; set; }

    public Guid ClothProductVariantId { get; set; }
    public ClothProductVariant? Variant { get; set; }

    public int PreviousStock { get; set; }
    public int AdjustedStock { get; set; }
    public int Difference { get; set; } // e.g. -2 or +5
    public decimal UnitCost { get; set; }
    public decimal TotalValueImpact { get; set; }
}

public class ClothStockLedger : BaseEntity, IBusinessScoped
{
    public Guid BusinessId { get; set; }

    public Guid ClothProductVariantId { get; set; }
    public ClothProductVariant? Variant { get; set; }

    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    
    // Types: "OpeningStock", "PurchaseInward", "SalePos", "SalesReturn", "BoxPacking", "BoxUnpacking", "Adjustment"
    public string TransactionType { get; set; } = "Adjustment";
    public string ReferenceNumber { get; set; } = string.Empty; // Document/Action reference

    public int QuantityIn { get; set; } = 0;
    public int QuantityOut { get; set; } = 0;
    public int RunningBalance { get; set; } = 0;
    public decimal UnitCost { get; set; } = 0;
    
    public string? Notes { get; set; }
}
