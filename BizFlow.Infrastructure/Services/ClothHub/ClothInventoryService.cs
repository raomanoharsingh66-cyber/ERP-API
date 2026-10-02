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

public class ClothInventoryService : IClothInventoryService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ClothInventoryService(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    private Guid GetEffectiveBusinessId()
    {
        return _currentUserService.BusinessId ?? Guid.Parse("22222222-2222-2222-2222-222222222222");
    }

    // --- 1. SIZE x COLOUR STOCK MATRIX ---
    public async Task<ClothStockMatrixDto?> GetStockMatrixAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var product = await _context.ClothProducts
            .AsNoTracking()
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.Variants)
                .ThenInclude(v => v.Size)
            .Include(p => p.Variants)
                .ThenInclude(v => v.Colour)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product == null)
            return null;

        var matrix = new ClothStockMatrixDto
        {
            ProductId = product.Id,
            ProductName = product.Name,
            SkuPrefix = product.SkuPrefix,
            BrandName = product.Brand?.Name ?? "General",
            CategoryName = product.Category?.Name ?? "Apparel",
            BaseMrp = product.BaseMrp,
            BaseSellingPrice = product.BaseSellingPrice,
            TotalStock = product.Variants.Sum(v => v.CurrentStock),
            TotalValuation = product.Variants.Sum(v => v.CurrentStock * v.SellingPrice)
        };

        // Extract distinct sizes, sorted by SortOrder then Name
        var distinctSizes = product.Variants
            .Where(v => v.Size != null)
            .Select(v => v.Size!)
            .GroupBy(s => s.Id)
            .Select(g => g.First())
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Name)
            .ToList();

        matrix.Sizes = distinctSizes.Select(s => new MatrixColumnSizeDto
        {
            SizeId = s.Id,
            SizeName = s.Name,
            SizeCode = s.Code,
            SortOrder = s.SortOrder,
            TotalSizeStock = product.Variants.Where(v => v.SizeId == s.Id).Sum(v => v.CurrentStock)
        }).ToList();

        // Extract distinct colours and build rows
        var distinctColours = product.Variants
            .Where(v => v.Colour != null)
            .Select(v => v.Colour!)
            .GroupBy(c => c.Id)
            .Select(g => g.First())
            .OrderBy(c => c.Name)
            .ToList();

        foreach (var col in distinctColours)
        {
            var colourVariants = product.Variants.Where(v => v.ColourId == col.Id).ToList();

            var row = new MatrixRowColourDto
            {
                ColourId = col.Id,
                ColourName = col.Name,
                ColourHex = col.HexCode,
                TotalColourStock = colourVariants.Sum(v => v.CurrentStock)
            };

            foreach (var size in distinctSizes)
            {
                var variant = colourVariants.FirstOrDefault(v => v.SizeId == size.Id);
                if (variant != null)
                {
                    row.Cells.Add(new MatrixCellVariantDto
                    {
                        VariantId = variant.Id,
                        SizeId = size.Id,
                        Sku = variant.Sku,
                        Barcode = variant.Barcode,
                        CurrentStock = variant.CurrentStock,
                        MinStockLevel = variant.MinStockLevel,
                        Mrp = variant.Mrp,
                        SellingPrice = variant.SellingPrice
                    });
                }
            }

            matrix.Rows.Add(row);
        }

        return matrix;
    }

    // --- 2. INVENTORY SUMMARY & LOW STOCK ALERTS ---
    public async Task<ClothInventorySummaryDto> GetInventorySummaryAsync(CancellationToken cancellationToken = default)
    {
        var variants = await _context.ClothProductVariants
            .AsNoTracking()
            .Include(v => v.Product)
                .ThenInclude(p => p!.Brand)
            .Include(v => v.Size)
            .Include(v => v.Colour)
            .ToListAsync(cancellationToken);

        var boxPacks = await _context.ClothBoxPacks
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var totalPieces = variants.Sum(v => v.CurrentStock);
        var totalValuation = variants.Sum(v => v.CurrentStock * v.SellingPrice);
        var lowStockVariants = variants.Where(v => v.CurrentStock <= v.MinStockLevel).ToList();

        // Brand-wise share
        var brandShare = variants
            .GroupBy(v => v.Product?.Brand?.Name ?? "Unbranded")
            .Select(g => new BrandStockShareDto
            {
                BrandName = g.Key,
                TotalPieces = g.Sum(v => v.CurrentStock),
                TotalValuation = g.Sum(v => v.CurrentStock * v.SellingPrice)
            })
            .OrderByDescending(b => b.TotalPieces)
            .Take(6)
            .ToList();

        // Size-wise breakdown
        var sizeShare = variants
            .GroupBy(v => v.Size?.Name ?? "Free")
            .Select(g => new SizeStockShareDto
            {
                SizeName = g.Key,
                TotalPieces = g.Sum(v => v.CurrentStock)
            })
            .OrderByDescending(s => s.TotalPieces)
            .Take(8)
            .ToList();

        var lowStockAlerts = lowStockVariants.Select(v => new LowStockAlertDto
        {
            VariantId = v.Id,
            ProductName = v.Product?.Name ?? "Garment Item",
            Sku = v.Sku,
            Barcode = v.Barcode,
            SizeName = v.Size?.Name ?? "",
            ColourName = v.Colour?.Name ?? "",
            CurrentStock = v.CurrentStock,
            MinStockLevel = v.MinStockLevel
        }).ToList();

        return new ClothInventorySummaryDto
        {
            TotalProducts = await _context.ClothProducts.CountAsync(cancellationToken),
            TotalVariants = variants.Count,
            TotalGarmentPieces = totalPieces,
            TotalBoxPacks = boxPacks.Sum(b => b.CurrentBoxStock),
            TotalInventoryValuation = totalValuation,
            LowStockItemsCount = lowStockVariants.Count,
            BrandBreakdown = brandShare,
            SizeBreakdown = sizeShare,
            LowStockAlerts = lowStockAlerts
        };
    }

    // --- 3. BOX & PACK ASSORTMENT MANAGEMENT ---
    public async Task<List<ClothBoxPackDto>> GetBoxPacksAsync(CancellationToken cancellationToken = default)
    {
        var packs = await _context.ClothBoxPacks
            .AsNoTracking()
            .Include(bp => bp.Product)
            .Include(bp => bp.Items)
                .ThenInclude(i => i.Variant)
                    .ThenInclude(v => v!.Size)
            .Include(bp => bp.Items)
                .ThenInclude(i => i.Variant)
                    .ThenInclude(v => v!.Colour)
            .OrderByDescending(bp => bp.CurrentBoxStock)
            .ToListAsync(cancellationToken);

        return packs.Select(p => new ClothBoxPackDto
        {
            Id = p.Id,
            ClothProductId = p.ClothProductId,
            ProductName = p.Product?.Name ?? "Garment Product",
            PackName = p.PackName,
            PackCode = p.PackCode,
            Barcode = p.Barcode,
            PiecesPerPack = p.PiecesPerPack,
            CostPrice = p.CostPrice,
            SellingPrice = p.SellingPrice,
            Mrp = p.Mrp,
            CurrentBoxStock = p.CurrentBoxStock,
            AssortmentDescription = p.AssortmentDescription,
            IsActive = p.IsActive,
            Items = p.Items.Select(i => new ClothBoxPackItemDto
            {
                Id = i.Id,
                ClothProductVariantId = i.ClothProductVariantId,
                VariantSku = i.Variant?.Sku ?? "",
                SizeName = i.Variant?.Size?.Name ?? "",
                ColourName = i.Variant?.Colour?.Name ?? "",
                QuantityPerBox = i.QuantityPerBox
            }).ToList()
        }).ToList();
    }

    public async Task<ClothBoxPackDto> CreateBoxPackAsync(CreateClothBoxPackDto dto, CancellationToken cancellationToken = default)
    {
        var businessId = GetEffectiveBusinessId();

        var boxPack = new ClothBoxPack
        {
            BusinessId = businessId,
            ClothProductId = dto.ClothProductId,
            PackName = dto.PackName.Trim(),
            PackCode = dto.PackCode.Trim().ToUpper(),
            Barcode = !string.IsNullOrWhiteSpace(dto.Barcode) ? dto.Barcode.Trim() : $"BOX{new Random().Next(10000000, 99999999)}",
            PiecesPerPack = dto.PiecesPerPack > 0 ? dto.PiecesPerPack : dto.Items.Sum(i => i.QuantityPerBox),
            CostPrice = dto.CostPrice,
            SellingPrice = dto.SellingPrice,
            Mrp = dto.Mrp,
            CurrentBoxStock = dto.InitialBoxStock,
            AssortmentDescription = dto.AssortmentDescription,
            IsActive = true
        };

        foreach (var itemDto in dto.Items)
        {
            boxPack.Items.Add(new ClothBoxPackItem
            {
                ClothProductVariantId = itemDto.ClothProductVariantId,
                QuantityPerBox = itemDto.QuantityPerBox
            });
        }

        _context.ClothBoxPacks.Add(boxPack);
        await _context.SaveChangesAsync(cancellationToken);

        return (await GetBoxPacksAsync(cancellationToken)).First(bp => bp.Id == boxPack.Id);
    }

    public async Task<bool> UnpackBoxAsync(Guid boxPackId, int boxesToUnpack, string? remarks = null, CancellationToken cancellationToken = default)
    {
        if (boxesToUnpack <= 0)
            throw new InvalidOperationException("Boxes to unpack must be at least 1.");

        var boxPack = await _context.ClothBoxPacks
            .Include(bp => bp.Items)
            .FirstOrDefaultAsync(bp => bp.Id == boxPackId, cancellationToken);

        if (boxPack == null)
            throw new KeyNotFoundException("Box/Pack not found.");

        if (boxPack.CurrentBoxStock < boxesToUnpack)
            throw new InvalidOperationException($"Insufficient box stock. Available: {boxPack.CurrentBoxStock}, Requested: {boxesToUnpack}.");

        // Decrement box stock
        boxPack.CurrentBoxStock -= boxesToUnpack;

        // Increase loose stock of each variant in the assortment and record in ledger
        var businessId = GetEffectiveBusinessId();
        var now = DateTime.UtcNow;

        foreach (var item in boxPack.Items)
        {
            var variant = await _context.ClothProductVariants.FirstOrDefaultAsync(v => v.Id == item.ClothProductVariantId, cancellationToken);
            if (variant != null)
            {
                var addedPieces = item.QuantityPerBox * boxesToUnpack;
                variant.CurrentStock += addedPieces;

                _context.ClothStockLedgers.Add(new ClothStockLedger
                {
                    BusinessId = businessId,
                    ClothProductVariantId = variant.Id,
                    TransactionDate = now,
                    TransactionType = "BoxUnpacking",
                    ReferenceNumber = boxPack.PackCode,
                    QuantityIn = addedPieces,
                    QuantityOut = 0,
                    RunningBalance = variant.CurrentStock,
                    UnitCost = variant.PurchasePrice,
                    Notes = $"Unpacked {boxesToUnpack} boxes of {boxPack.PackName}. {remarks}"
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> PackBoxAsync(Guid boxPackId, int boxesToPack, string? remarks = null, CancellationToken cancellationToken = default)
    {
        if (boxesToPack <= 0)
            throw new InvalidOperationException("Boxes to pack must be at least 1.");

        var boxPack = await _context.ClothBoxPacks
            .Include(bp => bp.Items)
            .FirstOrDefaultAsync(bp => bp.Id == boxPackId, cancellationToken);

        if (boxPack == null)
            throw new KeyNotFoundException("Box/Pack not found.");

        // Check if sufficient loose garments exist for each variant
        foreach (var item in boxPack.Items)
        {
            var variant = await _context.ClothProductVariants.FirstOrDefaultAsync(v => v.Id == item.ClothProductVariantId, cancellationToken);
            var required = item.QuantityPerBox * boxesToPack;
            if (variant == null || variant.CurrentStock < required)
            {
                var avail = variant?.CurrentStock ?? 0;
                throw new InvalidOperationException($"Cannot pack box. Insufficient loose stock for variant {variant?.Sku ?? "N/A"}. Required: {required}, Available: {avail}.");
            }
        }

        var businessId = GetEffectiveBusinessId();
        var now = DateTime.UtcNow;

        // Deduct loose pieces and record ledger
        foreach (var item in boxPack.Items)
        {
            var variant = await _context.ClothProductVariants.FirstAsync(v => v.Id == item.ClothProductVariantId, cancellationToken);
            var deductedPieces = item.QuantityPerBox * boxesToPack;
            variant.CurrentStock -= deductedPieces;

            _context.ClothStockLedgers.Add(new ClothStockLedger
            {
                BusinessId = businessId,
                ClothProductVariantId = variant.Id,
                TransactionDate = now,
                TransactionType = "BoxPacking",
                ReferenceNumber = boxPack.PackCode,
                QuantityIn = 0,
                QuantityOut = deductedPieces,
                RunningBalance = variant.CurrentStock,
                UnitCost = variant.PurchasePrice,
                Notes = $"Packed {boxesToPack} boxes of {boxPack.PackName}. {remarks}"
            });
        }

        // Increment intact box stock
        boxPack.CurrentBoxStock += boxesToPack;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    // --- 4. STOCK ADJUSTMENTS ---
    public async Task<List<ClothStockAdjustmentDto>> GetAdjustmentsAsync(CancellationToken cancellationToken = default)
    {
        var adjustments = await _context.ClothStockAdjustments
            .AsNoTracking()
            .Include(a => a.Items)
                .ThenInclude(i => i.Variant)
                    .ThenInclude(v => v!.Product)
            .Include(a => a.Items)
                .ThenInclude(i => i.Variant)
                    .ThenInclude(v => v!.Size)
            .Include(a => a.Items)
                .ThenInclude(i => i.Variant)
                    .ThenInclude(v => v!.Colour)
            .OrderByDescending(a => a.AdjustmentDate)
            .Take(50)
            .ToListAsync(cancellationToken);

        return adjustments.Select(a => new ClothStockAdjustmentDto
        {
            Id = a.Id,
            AdjustmentNumber = a.AdjustmentNumber,
            AdjustmentDate = a.AdjustmentDate,
            Reason = a.Reason,
            TotalItemsCount = a.TotalItemsCount,
            TotalNetQuantityDiff = a.TotalNetQuantityDiff,
            TotalValueImpact = a.TotalValueImpact,
            Remarks = a.Remarks,
            Items = a.Items.Select(i => new ClothStockAdjustmentItemDto
            {
                Id = i.Id,
                ClothProductVariantId = i.ClothProductVariantId,
                VariantSku = i.Variant?.Sku ?? "",
                ProductName = i.Variant?.Product?.Name ?? "",
                SizeName = i.Variant?.Size?.Name ?? "",
                ColourName = i.Variant?.Colour?.Name ?? "",
                PreviousStock = i.PreviousStock,
                AdjustedStock = i.AdjustedStock,
                Difference = i.Difference,
                UnitCost = i.UnitCost,
                TotalValueImpact = i.TotalValueImpact
            }).ToList()
        }).ToList();
    }

    public async Task<ClothStockAdjustmentDto> CreateAdjustmentAsync(CreateClothStockAdjustmentDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.Items == null || dto.Items.Count == 0)
            throw new InvalidOperationException("At least 1 item must be specified for stock adjustment.");

        var businessId = GetEffectiveBusinessId();
        var now = DateTime.UtcNow;

        var count = await _context.ClothStockAdjustments.CountAsync(cancellationToken);
        var adjNumber = $"ADJ-{now:yyyyMMdd}-{(count + 1):D4}";

        var adjustment = new ClothStockAdjustment
        {
            BusinessId = businessId,
            AdjustmentNumber = adjNumber,
            AdjustmentDate = now,
            Reason = dto.Reason,
            Remarks = dto.Remarks,
            TotalItemsCount = dto.Items.Count
        };

        var totalNetDiff = 0;
        decimal totalValImpact = 0;

        foreach (var itemDto in dto.Items)
        {
            var variant = await _context.ClothProductVariants.FirstOrDefaultAsync(v => v.Id == itemDto.ClothProductVariantId, cancellationToken);
            if (variant == null) continue;

            var prevStock = variant.CurrentStock;
            var newStock = Math.Max(0, itemDto.AdjustedStock);
            var diff = newStock - prevStock;
            var valImpact = diff * variant.PurchasePrice;

            totalNetDiff += diff;
            totalValImpact += valImpact;

            // Apply stock change
            variant.CurrentStock = newStock;

            adjustment.Items.Add(new ClothStockAdjustmentItem
            {
                ClothProductVariantId = variant.Id,
                PreviousStock = prevStock,
                AdjustedStock = newStock,
                Difference = diff,
                UnitCost = variant.PurchasePrice,
                TotalValueImpact = valImpact
            });

            // Add Stock Ledger entry
            _context.ClothStockLedgers.Add(new ClothStockLedger
            {
                BusinessId = businessId,
                ClothProductVariantId = variant.Id,
                TransactionDate = now,
                TransactionType = "Adjustment",
                ReferenceNumber = adjNumber,
                QuantityIn = diff > 0 ? diff : 0,
                QuantityOut = diff < 0 ? Math.Abs(diff) : 0,
                RunningBalance = variant.CurrentStock,
                UnitCost = variant.PurchasePrice,
                Notes = $"Stock Adjustment ({dto.Reason}): from {prevStock} to {newStock} Pcs. {dto.Remarks}"
            });
        }

        adjustment.TotalNetQuantityDiff = totalNetDiff;
        adjustment.TotalValueImpact = totalValImpact;

        _context.ClothStockAdjustments.Add(adjustment);
        await _context.SaveChangesAsync(cancellationToken);

        return (await GetAdjustmentsAsync(cancellationToken)).First(a => a.Id == adjustment.Id);
    }

    // --- 5. STOCK LEDGER AUDIT TRAIL ---
    public async Task<List<ClothStockLedgerDto>> GetStockLedgerAsync(Guid? variantId = null, string? transactionType = null, CancellationToken cancellationToken = default)
    {
        var query = _context.ClothStockLedgers
            .AsNoTracking()
            .Include(sl => sl.Variant)
                .ThenInclude(v => v!.Product)
            .Include(sl => sl.Variant)
                .ThenInclude(v => v!.Size)
            .Include(sl => sl.Variant)
                .ThenInclude(v => v!.Colour)
            .AsQueryable();

        if (variantId.HasValue)
        {
            query = query.Where(sl => sl.ClothProductVariantId == variantId.Value);
        }

        if (!string.IsNullOrWhiteSpace(transactionType))
        {
            query = query.Where(sl => sl.TransactionType == transactionType);
        }

        var entries = await query
            .OrderByDescending(sl => sl.TransactionDate)
            .Take(100)
            .ToListAsync(cancellationToken);

        return entries.Select(sl => new ClothStockLedgerDto
        {
            Id = sl.Id,
            ClothProductVariantId = sl.ClothProductVariantId,
            VariantSku = sl.Variant?.Sku ?? "",
            ProductName = sl.Variant?.Product?.Name ?? "",
            SizeName = sl.Variant?.Size?.Name ?? "",
            ColourName = sl.Variant?.Colour?.Name ?? "",
            TransactionDate = sl.TransactionDate,
            TransactionType = sl.TransactionType,
            ReferenceNumber = sl.ReferenceNumber,
            QuantityIn = sl.QuantityIn,
            QuantityOut = sl.QuantityOut,
            RunningBalance = sl.RunningBalance,
            UnitCost = sl.UnitCost,
            Notes = sl.Notes
        }).ToList();
    }
}
