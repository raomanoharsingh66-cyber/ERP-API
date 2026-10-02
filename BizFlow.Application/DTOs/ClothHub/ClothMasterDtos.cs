using System;
using System.Collections.Generic;

namespace BizFlow.Application.DTOs.ClothHub;

public class ClothBrandDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public int ProductCount { get; set; }
}

public class CreateClothBrandDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class ClothCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Gender { get; set; } = "Unisex";
    public bool IsActive { get; set; } = true;
}

public class CreateClothCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Gender { get; set; } = "Unisex";
}

public class ClothSizeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string CategoryType { get; set; } = "Standard";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CreateClothSizeDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string CategoryType { get; set; } = "Standard";
    public int SortOrder { get; set; }
}

public class ClothColourDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string HexCode { get; set; } = "#000000";
    public string? PaletteGroup { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CreateClothColourDto
{
    public string Name { get; set; } = string.Empty;
    public string HexCode { get; set; } = "#000000";
    public string? PaletteGroup { get; set; }
}

public class ClothFabricDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Composition { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CreateClothFabricDto
{
    public string Name { get; set; } = string.Empty;
    public string? Composition { get; set; }
}

public class ClothProductVariantDto
{
    public Guid Id { get; set; }
    public Guid ClothProductId { get; set; }
    public Guid SizeId { get; set; }
    public string SizeName { get; set; } = string.Empty;
    public Guid ColourId { get; set; }
    public string ColourName { get; set; } = string.Empty;
    public string ColourHex { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public decimal Mrp { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public int CurrentStock { get; set; }
    public int MinStockLevel { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ClothProductDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SkuPrefix { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid BrandId { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public Guid? FabricId { get; set; }
    public string? FabricName { get; set; }
    public string Gender { get; set; } = "Unisex";
    public string? DesignNumber { get; set; }
    public string HsnCode { get; set; } = "6109";
    public decimal GstRate { get; set; } = 5.0m;
    public decimal BaseMrp { get; set; }
    public decimal BasePurchasePrice { get; set; }
    public decimal BaseSellingPrice { get; set; }
    public int MinStockLevel { get; set; }
    public int TotalStock { get; set; }
    public int TotalVariantsCount { get; set; }
    public bool IsActive { get; set; } = true;
    public List<ClothProductVariantDto> Variants { get; set; } = new();
}

public class CreateClothProductRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string SkuPrefix { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid BrandId { get; set; }
    public Guid CategoryId { get; set; }
    public Guid? FabricId { get; set; }
    public string Gender { get; set; } = "Unisex";
    public string? DesignNumber { get; set; }
    public string HsnCode { get; set; } = "6109";
    public decimal GstRate { get; set; } = 5.0m;
    public decimal BaseMrp { get; set; }
    public decimal BasePurchasePrice { get; set; }
    public decimal BaseSellingPrice { get; set; }
    public int MinStockLevel { get; set; } = 10;
    
    // Matrix Selection: Every Size x Colour combination will be generated
    public List<Guid> SelectedSizeIds { get; set; } = new();
    public List<Guid> SelectedColourIds { get; set; } = new();
}
