using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.DTOs.ClothHub;
using BizFlow.Domain.Entities.ClothHub;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Services.ClothHub;

public class ClothProductService : IClothProductService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ClothProductService(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    private Guid GetEffectiveBusinessId()
    {
        return _currentUserService.BusinessId ?? Guid.Parse("22222222-2222-2222-2222-222222222222");
    }

    public async Task<List<ClothProductDetailDto>> GetProductsAsync(string? search = null, Guid? brandId = null, Guid? categoryId = null, CancellationToken cancellationToken = default)
    {
        await EnsureSampleProductsAsync(cancellationToken);

        var query = _context.ClothProducts
            .AsNoTracking()
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.Fabric)
            .Include(p => p.Variants)
                .ThenInclude(v => v.Size)
            .Include(p => p.Variants)
                .ThenInclude(v => v.Colour)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(s) || p.SkuPrefix.ToLower().Contains(s) || (p.DesignNumber != null && p.DesignNumber.ToLower().Contains(s)));
        }

        if (brandId.HasValue && brandId.Value != Guid.Empty)
        {
            query = query.Where(p => p.BrandId == brandId.Value);
        }

        if (categoryId.HasValue && categoryId.Value != Guid.Empty)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        var products = await query.OrderByDescending(p => p.CreatedOn).ToListAsync(cancellationToken);

        return products.Select(MapToDetailDto).ToList();
    }

    public async Task<ClothProductDetailDto?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _context.ClothProducts
            .AsNoTracking()
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.Fabric)
            .Include(p => p.Variants)
                .ThenInclude(v => v.Size)
            .Include(p => p.Variants)
                .ThenInclude(v => v.Colour)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        return product != null ? MapToDetailDto(product) : null;
    }

    public async Task<ClothProductDetailDto> CreateProductWithVariantsAsync(CreateClothProductRequestDto dto, CancellationToken cancellationToken = default)
    {
        var bid = GetEffectiveBusinessId();

        // 1. Create Base Product
        var product = new ClothProduct
        {
            BusinessId = bid,
            Name = dto.Name.Trim(),
            SkuPrefix = dto.SkuPrefix.Trim().ToUpper(),
            Description = dto.Description,
            BrandId = dto.BrandId,
            CategoryId = dto.CategoryId,
            FabricId = dto.FabricId,
            Gender = dto.Gender,
            DesignNumber = dto.DesignNumber,
            HsnCode = string.IsNullOrWhiteSpace(dto.HsnCode) ? "6109" : dto.HsnCode,
            GstRate = dto.GstRate > 0 ? dto.GstRate : 5.0m,
            BaseMrp = dto.BaseMrp,
            BasePurchasePrice = dto.BasePurchasePrice,
            BaseSellingPrice = dto.BaseSellingPrice,
            MinStockLevel = dto.MinStockLevel > 0 ? dto.MinStockLevel : 10,
            IsActive = true
        };

        // 2. Load Selected Sizes and Colours to Generate Matrix
        var sizes = await _context.ClothSizes
            .Where(s => dto.SelectedSizeIds.Contains(s.Id))
            .OrderBy(s => s.SortOrder)
            .ToListAsync(cancellationToken);

        var colours = await _context.ClothColours
            .Where(c => dto.SelectedColourIds.Contains(c.Id))
            .ToListAsync(cancellationToken);

        // Fallbacks if user didn't specify
        if (sizes.Count == 0)
        {
            sizes = await _context.ClothSizes.Take(3).ToListAsync(cancellationToken);
        }
        if (colours.Count == 0)
        {
            colours = await _context.ClothColours.Take(2).ToListAsync(cancellationToken);
        }

        // 3. Matrix Multiplier: Generate Size x Colour Variants
        int variantIndex = 1;
        foreach (var colour in colours)
        {
            var colourCode = colour.Name.Substring(0, Math.Min(3, colour.Name.Length)).ToUpper().Replace(" ", "");
            
            foreach (var size in sizes)
            {
                var sizeCode = size.Code.ToUpper();
                var sku = $"{product.SkuPrefix}-{colourCode}-{sizeCode}";
                var barcode = GenerateEan13Barcode(bid, variantIndex++);

                product.Variants.Add(new ClothProductVariant
                {
                    BusinessId = bid,
                    SizeId = size.Id,
                    ColourId = colour.Id,
                    Sku = sku,
                    Barcode = barcode,
                    Mrp = product.BaseMrp,
                    PurchasePrice = product.BasePurchasePrice,
                    SellingPrice = product.BaseSellingPrice,
                    CurrentStock = 10, // Initial seed inventory per variant
                    MinStockLevel = 4,
                    IsActive = true
                });
            }
        }

        _context.ClothProducts.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        return await GetProductByIdAsync(product.Id, cancellationToken) ?? MapToDetailDto(product);
    }

    public async Task<bool> UpdateVariantStockAsync(Guid variantId, int newStock, CancellationToken cancellationToken = default)
    {
        var variant = await _context.ClothProductVariants.FirstOrDefaultAsync(v => v.Id == variantId, cancellationToken);
        if (variant == null) return false;

        variant.CurrentStock = Math.Max(0, newStock);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string GenerateEan13Barcode(Guid businessId, int index)
    {
        // 890 (India) + 6 digits + 3 digit sequence + 1 checksum
        var hash = Math.Abs(businessId.GetHashCode()) % 900000 + 100000;
        var seq = (index % 900) + 100;
        var raw12 = $"890{hash}{seq}";
        
        // Calculate EAN-13 Luhn Checksum
        int sum = 0;
        for (int i = 0; i < 12; i++)
        {
            int d = raw12[i] - '0';
            sum += (i % 2 == 0) ? d : d * 3;
        }
        int check = (10 - (sum % 10)) % 10;
        return $"{raw12}{check}";
    }

    private static ClothProductDetailDto MapToDetailDto(ClothProduct p)
    {
        var variants = p.Variants.Select(v => new ClothProductVariantDto
        {
            Id = v.Id,
            ClothProductId = v.ClothProductId,
            SizeId = v.SizeId,
            SizeName = v.Size?.Name ?? "Standard",
            ColourId = v.ColourId,
            ColourName = v.Colour?.Name ?? "Default",
            ColourHex = v.Colour?.HexCode ?? "#000000",
            Sku = v.Sku,
            Barcode = v.Barcode,
            Mrp = v.Mrp,
            PurchasePrice = v.PurchasePrice,
            SellingPrice = v.SellingPrice,
            CurrentStock = v.CurrentStock,
            MinStockLevel = v.MinStockLevel,
            IsActive = v.IsActive
        }).ToList();

        return new ClothProductDetailDto
        {
            Id = p.Id,
            Name = p.Name,
            SkuPrefix = p.SkuPrefix,
            Description = p.Description,
            BrandId = p.BrandId,
            BrandName = p.Brand?.Name ?? "Unbranded",
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.Name ?? "General Apparel",
            FabricId = p.FabricId,
            FabricName = p.Fabric?.Name,
            Gender = p.Gender,
            DesignNumber = p.DesignNumber,
            HsnCode = p.HsnCode,
            GstRate = p.GstRate,
            BaseMrp = p.BaseMrp,
            BasePurchasePrice = p.BasePurchasePrice,
            BaseSellingPrice = p.BaseSellingPrice,
            MinStockLevel = p.MinStockLevel,
            TotalStock = variants.Sum(v => v.CurrentStock),
            TotalVariantsCount = variants.Count,
            IsActive = p.IsActive,
            Variants = variants
        };
    }

    private async Task EnsureSampleProductsAsync(CancellationToken cancellationToken)
    {
        if (await _context.ClothProducts.AnyAsync(cancellationToken)) return;

        var bid = GetEffectiveBusinessId();
        var brands = await _context.ClothBrands.ToListAsync(cancellationToken);
        var categories = await _context.ClothCategories.ToListAsync(cancellationToken);
        var sizes = await _context.ClothSizes.Where(s => s.CategoryType == "Standard").Take(4).ToListAsync(cancellationToken);
        var colours = await _context.ClothColours.Take(3).ToListAsync(cancellationToken);

        if (brands.Count == 0 || categories.Count == 0 || sizes.Count == 0 || colours.Count == 0) return;

        var sample1 = new ClothProduct
        {
            BusinessId = bid,
            Name = "Men's Classic Cotton Formal Shirt",
            SkuPrefix = "AS-FSHIRT",
            BrandId = brands[0].Id,
            CategoryId = categories[0].Id,
            Gender = "Men's",
            DesignNumber = "DS-FORMAL-01",
            HsnCode = "6109",
            GstRate = 5.0m,
            BaseMrp = 1499m,
            BasePurchasePrice = 650m,
            BaseSellingPrice = 1199m,
            MinStockLevel = 15,
            IsActive = true
        };

        int idx = 1;
        foreach (var col in colours)
        {
            var colCode = col.Name.Substring(0, 3).ToUpper();
            foreach (var sz in sizes)
            {
                sample1.Variants.Add(new ClothProductVariant
                {
                    BusinessId = bid,
                    SizeId = sz.Id,
                    ColourId = col.Id,
                    Sku = $"AS-FSHIRT-{colCode}-{sz.Code}",
                    Barcode = GenerateEan13Barcode(bid, idx++),
                    Mrp = 1499m,
                    PurchasePrice = 650m,
                    SellingPrice = 1199m,
                    CurrentStock = 12,
                    MinStockLevel = 4
                });
            }
        }

        _context.ClothProducts.Add(sample1);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
