using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.DTOs.ClothHub;
using BizFlow.Domain.Entities.ClothHub;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Services.ClothHub;

public class ClothMasterService : IClothMasterService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ClothMasterService(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    private Guid GetEffectiveBusinessId()
    {
        return _currentUserService.BusinessId ?? Guid.Parse("22222222-2222-2222-2222-222222222222");
    }

    public async Task<List<ClothBrandDto>> GetBrandsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDefaultsAsync(cancellationToken);

        return await _context.ClothBrands
            .AsNoTracking()
            .OrderBy(b => b.Name)
            .Select(b => new ClothBrandDto
            {
                Id = b.Id,
                Name = b.Name,
                Code = b.Code,
                Description = b.Description,
                LogoUrl = b.LogoUrl,
                IsActive = b.IsActive,
                ProductCount = _context.ClothProducts.Count(p => p.BrandId == b.Id)
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ClothBrandDto> CreateBrandAsync(CreateClothBrandDto dto, CancellationToken cancellationToken = default)
    {
        var brand = new ClothBrand
        {
            BusinessId = GetEffectiveBusinessId(),
            Name = dto.Name.Trim(),
            Code = dto.Code.Trim().ToUpper(),
            Description = dto.Description,
            IsActive = true
        };

        _context.ClothBrands.Add(brand);
        await _context.SaveChangesAsync(cancellationToken);

        return new ClothBrandDto
        {
            Id = brand.Id,
            Name = brand.Name,
            Code = brand.Code,
            Description = brand.Description,
            IsActive = brand.IsActive,
            ProductCount = 0
        };
    }

    public async Task<List<ClothCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDefaultsAsync(cancellationToken);

        return await _context.ClothCategories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new ClothCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Code = c.Code,
                Gender = c.Gender,
                IsActive = c.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ClothCategoryDto> CreateCategoryAsync(CreateClothCategoryDto dto, CancellationToken cancellationToken = default)
    {
        var category = new ClothCategory
        {
            BusinessId = GetEffectiveBusinessId(),
            Name = dto.Name.Trim(),
            Code = dto.Code.Trim().ToUpper(),
            Gender = dto.Gender,
            IsActive = true
        };

        _context.ClothCategories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);

        return new ClothCategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Code = category.Code,
            Gender = category.Gender,
            IsActive = category.IsActive
        };
    }

    public async Task<List<ClothSizeDto>> GetSizesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDefaultsAsync(cancellationToken);

        return await _context.ClothSizes
            .AsNoTracking()
            .OrderBy(s => s.SortOrder)
            .Select(s => new ClothSizeDto
            {
                Id = s.Id,
                Name = s.Name,
                Code = s.Code,
                CategoryType = s.CategoryType,
                SortOrder = s.SortOrder,
                IsActive = s.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ClothSizeDto> CreateSizeAsync(CreateClothSizeDto dto, CancellationToken cancellationToken = default)
    {
        var size = new ClothSize
        {
            BusinessId = GetEffectiveBusinessId(),
            Name = dto.Name.Trim(),
            Code = dto.Code.Trim().ToUpper(),
            CategoryType = dto.CategoryType,
            SortOrder = dto.SortOrder,
            IsActive = true
        };

        _context.ClothSizes.Add(size);
        await _context.SaveChangesAsync(cancellationToken);

        return new ClothSizeDto
        {
            Id = size.Id,
            Name = size.Name,
            Code = size.Code,
            CategoryType = size.CategoryType,
            SortOrder = size.SortOrder,
            IsActive = size.IsActive
        };
    }

    public async Task<List<ClothColourDto>> GetColoursAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDefaultsAsync(cancellationToken);

        return await _context.ClothColours
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new ClothColourDto
            {
                Id = c.Id,
                Name = c.Name,
                HexCode = c.HexCode,
                PaletteGroup = c.PaletteGroup,
                IsActive = c.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ClothColourDto> CreateColourAsync(CreateClothColourDto dto, CancellationToken cancellationToken = default)
    {
        var colour = new ClothColour
        {
            BusinessId = GetEffectiveBusinessId(),
            Name = dto.Name.Trim(),
            HexCode = dto.HexCode.Trim(),
            PaletteGroup = dto.PaletteGroup,
            IsActive = true
        };

        _context.ClothColours.Add(colour);
        await _context.SaveChangesAsync(cancellationToken);

        return new ClothColourDto
        {
            Id = colour.Id,
            Name = colour.Name,
            HexCode = colour.HexCode,
            PaletteGroup = colour.PaletteGroup,
            IsActive = colour.IsActive
        };
    }

    public async Task<List<ClothFabricDto>> GetFabricsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDefaultsAsync(cancellationToken);

        return await _context.ClothFabrics
            .AsNoTracking()
            .OrderBy(f => f.Name)
            .Select(f => new ClothFabricDto
            {
                Id = f.Id,
                Name = f.Name,
                Composition = f.Composition,
                IsActive = f.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ClothFabricDto> CreateFabricAsync(CreateClothFabricDto dto, CancellationToken cancellationToken = default)
    {
        var fabric = new ClothFabric
        {
            BusinessId = GetEffectiveBusinessId(),
            Name = dto.Name.Trim(),
            Composition = dto.Composition,
            IsActive = true
        };

        _context.ClothFabrics.Add(fabric);
        await _context.SaveChangesAsync(cancellationToken);

        return new ClothFabricDto
        {
            Id = fabric.Id,
            Name = fabric.Name,
            Composition = fabric.Composition,
            IsActive = fabric.IsActive
        };
    }

    private async Task EnsureDefaultsAsync(CancellationToken cancellationToken)
    {
        var bid = GetEffectiveBusinessId();

        // Seed Brands if empty
        if (!await _context.ClothBrands.AnyAsync(cancellationToken))
        {
            _context.ClothBrands.AddRange(new[]
            {
                new ClothBrand { BusinessId = bid, Name = "Allen Solly", Code = "AS", Description = "Premium Casuals & Formalwear" },
                new ClothBrand { BusinessId = bid, Name = "Manyavar", Code = "MNY", Description = "Celebration & Ethnic Kurtas" },
                new ClothBrand { BusinessId = bid, Name = "Raymond", Code = "RYM", Description = "Fine Suiting, Shirting & Trousers" },
                new ClothBrand { BusinessId = bid, Name = "Levi's", Code = "LEV", Description = "Authentic Denim & Streetwear" },
                new ClothBrand { BusinessId = bid, Name = "Zara Kids", Code = "ZRK", Description = "Modern Children's Apparel" },
                new ClothBrand { BusinessId = bid, Name = "FabIndia", Code = "FBI", Description = "Handcrafted Ethnic Sarees & Kurtis" }
            });
        }

        // Seed Sizes if empty
        if (!await _context.ClothSizes.AnyAsync(cancellationToken))
        {
            _context.ClothSizes.AddRange(new[]
            {
                new ClothSize { BusinessId = bid, Name = "S", Code = "S", CategoryType = "Standard", SortOrder = 1 },
                new ClothSize { BusinessId = bid, Name = "M", Code = "M", CategoryType = "Standard", SortOrder = 2 },
                new ClothSize { BusinessId = bid, Name = "L", Code = "L", CategoryType = "Standard", SortOrder = 3 },
                new ClothSize { BusinessId = bid, Name = "XL", Code = "XL", CategoryType = "Standard", SortOrder = 4 },
                new ClothSize { BusinessId = bid, Name = "XXL", Code = "XXL", CategoryType = "Standard", SortOrder = 5 },
                new ClothSize { BusinessId = bid, Name = "30", Code = "30", CategoryType = "Waist", SortOrder = 10 },
                new ClothSize { BusinessId = bid, Name = "32", Code = "32", CategoryType = "Waist", SortOrder = 11 },
                new ClothSize { BusinessId = bid, Name = "34", Code = "34", CategoryType = "Waist", SortOrder = 12 },
                new ClothSize { BusinessId = bid, Name = "36", Code = "36", CategoryType = "Waist", SortOrder = 13 },
                new ClothSize { BusinessId = bid, Name = "Free Size", Code = "FS", CategoryType = "FreeSize", SortOrder = 20 }
            });
        }

        // Seed Colours if empty
        if (!await _context.ClothColours.AnyAsync(cancellationToken))
        {
            _context.ClothColours.AddRange(new[]
            {
                new ClothColour { BusinessId = bid, Name = "Jet Black", HexCode = "#111827", PaletteGroup = "Dark" },
                new ClothColour { BusinessId = bid, Name = "Crisp White", HexCode = "#F8FAFC", PaletteGroup = "Light" },
                new ClothColour { BusinessId = bid, Name = "Navy Blue", HexCode = "#1E3A8A", PaletteGroup = "Dark" },
                new ClothColour { BusinessId = bid, Name = "Olive Green", HexCode = "#4D7C0F", PaletteGroup = "Earth" },
                new ClothColour { BusinessId = bid, Name = "Deep Maroon", HexCode = "#881337", PaletteGroup = "Rich" },
                new ClothColour { BusinessId = bid, Name = "Sky Blue", HexCode = "#38BDF8", PaletteGroup = "Pastel" },
                new ClothColour { BusinessId = bid, Name = "Heather Grey", HexCode = "#64748B", PaletteGroup = "Neutral" },
                new ClothColour { BusinessId = bid, Name = "Mustard Yellow", HexCode = "#CA8A04", PaletteGroup = "Warm" }
            });
        }

        // Seed Categories if empty
        if (!await _context.ClothCategories.AnyAsync(cancellationToken))
        {
            _context.ClothCategories.AddRange(new[]
            {
                new ClothCategory { BusinessId = bid, Name = "Formal Shirts", Code = "FSHIRT", Gender = "Men's" },
                new ClothCategory { BusinessId = bid, Name = "Casual T-Shirts", Code = "TSHIRT", Gender = "Unisex" },
                new ClothCategory { BusinessId = bid, Name = "Denim Jeans", Code = "JEANS", Gender = "Unisex" },
                new ClothCategory { BusinessId = bid, Name = "Ethnic Kurtis", Code = "KURTI", Gender = "Women's" },
                new ClothCategory { BusinessId = bid, Name = "Designer Sarees", Code = "SAREE", Gender = "Women's" },
                new ClothCategory { BusinessId = bid, Name = "Formal Trousers", Code = "TROUSER", Gender = "Men's" },
                new ClothCategory { BusinessId = bid, Name = "Kidswear Sets", Code = "KIDS", Gender = "Kids" }
            });
        }

        // Seed Fabrics if empty
        if (!await _context.ClothFabrics.AnyAsync(cancellationToken))
        {
            _context.ClothFabrics.AddRange(new[]
            {
                new ClothFabric { BusinessId = bid, Name = "100% Combed Cotton", Composition = "100% Cotton" },
                new ClothFabric { BusinessId = bid, Name = "Linen Blend", Composition = "60% Linen, 40% Cotton" },
                new ClothFabric { BusinessId = bid, Name = "Stretch Denim", Composition = "98% Cotton, 2% Elastane" },
                new ClothFabric { BusinessId = bid, Name = "Pure Silk Zari", Composition = "100% Silk" },
                new ClothFabric { BusinessId = bid, Name = "Rayon Crepe", Composition = "100% Viscose Rayon" }
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
