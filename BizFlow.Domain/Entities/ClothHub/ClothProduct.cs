using System;
using System.Collections.Generic;
using BizFlow.Domain.Common;

namespace BizFlow.Domain.Entities.ClothHub;

public class ClothProduct : BaseEntity, IBusinessScoped
{
    public Guid BusinessId { get; set; }
    public string Name { get; set; } = string.Empty; // e.g. "Men's Cotton Polo T-Shirt"
    public string SkuPrefix { get; set; } = string.Empty; // "TS-POLO"
    public string? Description { get; set; }
    
    public Guid BrandId { get; set; }
    public ClothBrand? Brand { get; set; }

    public Guid CategoryId { get; set; }
    public ClothCategory? Category { get; set; }

    public Guid? FabricId { get; set; }
    public ClothFabric? Fabric { get; set; }

    public string? DesignNumber { get; set; }
    public string Gender { get; set; } = "Unisex"; // Men's, Women's, Kids, Unisex
    public string? Season { get; set; }
    public string? Collection { get; set; }
    public string HsnCode { get; set; } = "6109"; // Garments HSN
    public decimal GstRate { get; set; } = 5.0m; // 5% or 12% in India
    public string UnitOfMeasurement { get; set; } = "PCS";

    public decimal BaseMrp { get; set; }
    public decimal BasePurchasePrice { get; set; }
    public decimal BaseSellingPrice { get; set; }
    public decimal? WholesalePrice { get; set; }

    public int MinStockLevel { get; set; } = 10;
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation to Size-Colour Variants
    public ICollection<ClothProductVariant> Variants { get; set; } = new List<ClothProductVariant>();
}

public class ClothProductVariant : BaseEntity, IBusinessScoped
{
    public Guid BusinessId { get; set; }
    public Guid ClothProductId { get; set; }
    public ClothProduct? Product { get; set; }

    public Guid SizeId { get; set; }
    public ClothSize? Size { get; set; }

    public Guid ColourId { get; set; }
    public ClothColour? Colour { get; set; }

    public string Sku { get; set; } = string.Empty; // e.g. "TS-POLO-BLK-M"
    public string Barcode { get; set; } = string.Empty; // EAN-13 format barcode
    
    public decimal Mrp { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }

    public int CurrentStock { get; set; } = 0;
    public int ReservedStock { get; set; } = 0;
    public int MinStockLevel { get; set; } = 5;

    public bool IsActive { get; set; } = true;
}
