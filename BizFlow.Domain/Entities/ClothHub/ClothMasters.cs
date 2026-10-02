using System;
using System.Collections.Generic;
using BizFlow.Domain.Common;

namespace BizFlow.Domain.Entities.ClothHub;

public class ClothBrand : BaseEntity, IBusinessScoped
{
    public Guid BusinessId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ClothCategory : BaseEntity, IBusinessScoped
{
    public Guid BusinessId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Gender { get; set; } = "Unisex"; // Men's, Women's, Kids, Unisex
    public Guid? ParentCategoryId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ClothSize : BaseEntity, IBusinessScoped
{
    public Guid BusinessId { get; set; }
    public string Name { get; set; } = string.Empty; // e.g. "S", "M", "L", "XL", "32", "34"
    public string Code { get; set; } = string.Empty;
    public string CategoryType { get; set; } = "Standard"; // Standard, Waist, Kids, FreeSize
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ClothColour : BaseEntity, IBusinessScoped
{
    public Guid BusinessId { get; set; }
    public string Name { get; set; } = string.Empty; // "Jet Black", "Crisp White", "Navy Blue"
    public string HexCode { get; set; } = "#000000";
    public string? PaletteGroup { get; set; } // Dark, Light, Pastel, Primary
    public bool IsActive { get; set; } = true;
}

public class ClothDesign : BaseEntity, IBusinessScoped
{
    public Guid BusinessId { get; set; }
    public string DesignNumber { get; set; } = string.Empty; // "DS-101", "CHECK-04"
    public string Pattern { get; set; } = "Plain"; // Plain, Checked, Printed, Striped, Embroidered
    public string Fit { get; set; } = "Regular"; // Slim Fit, Regular Fit, Relaxed, Oversized
    public string? Season { get; set; } // Summer, Winter, Festive, All Season
    public bool IsActive { get; set; } = true;
}

public class ClothFabric : BaseEntity, IBusinessScoped
{
    public Guid BusinessId { get; set; }
    public string Name { get; set; } = string.Empty; // "100% Combed Cotton", "Linen Blend", "Silk Satin", "Stretch Denim"
    public string? Composition { get; set; }
    public bool IsActive { get; set; } = true;
}
