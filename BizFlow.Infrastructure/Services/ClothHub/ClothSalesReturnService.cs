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

public class ClothSalesReturnService : IClothSalesReturnService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ClothSalesReturnService(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<List<ClothSalesReturnDto>> GetReturnsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? search = null,
        string? returnType = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ClothSalesReturns
            .Include(r => r.Items)
            .AsNoTracking()
            .AsQueryable();

        if (fromDate.HasValue)
        {
            var f = fromDate.Value.Date;
            query = query.Where(r => r.ReturnDate >= f);
        }

        if (toDate.HasValue)
        {
            var t = toDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(r => r.ReturnDate <= t);
        }

        if (!string.IsNullOrWhiteSpace(returnType))
        {
            query = query.Where(r => r.ReturnType.ToLower() == returnType.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(r =>
                r.ReturnNumber.ToLower().Contains(term) ||
                (r.OriginalInvoiceNumber != null && r.OriginalInvoiceNumber.ToLower().Contains(term)) ||
                r.CustomerName.ToLower().Contains(term) ||
                (r.CustomerPhone != null && r.CustomerPhone.Contains(term)) ||
                (r.CreditNoteNumber != null && r.CreditNoteNumber.ToLower().Contains(term)));
        }

        var list = await query
            .OrderByDescending(r => r.ReturnDate)
            .ToListAsync(cancellationToken);

        return list.Select(MapToDto).ToList();
    }

    public async Task<ClothSalesReturnDto?> GetReturnByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ClothSalesReturns
            .Include(r => r.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        return entity == null ? null : MapToDto(entity);
    }

    public async Task<ClothSalesReturnDto> CreateReturnOrExchangeAsync(
        CreateClothSalesReturnDto dto,
        CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.BusinessId ?? Guid.Empty;
        var today = DateTime.UtcNow;

        var isExchange = string.Equals(dto.ReturnType, "Exchange", StringComparison.OrdinalIgnoreCase);

        // 1. Generate Return / Exchange Sequence Number
        var todayPrefix = isExchange ? $"EXC-{today:yyyyMMdd}" : $"RET-{today:yyyyMMdd}";
        var existingCount = await _context.ClothSalesReturns
            .IgnoreQueryFilters()
            .CountAsync(r => r.ReturnNumber.StartsWith(todayPrefix), cancellationToken);

        var returnNumber = $"{todayPrefix}-{(existingCount + 1):D4}";

        // 2. Generate Credit Note sequence if credit note settlement
        string? creditNoteNumber = null;
        if (string.Equals(dto.SettlementMode, "CreditNote", StringComparison.OrdinalIgnoreCase) ||
            (!isExchange && string.Equals(dto.SettlementMode, "StoreCredit", StringComparison.OrdinalIgnoreCase)))
        {
            var cnPrefix = $"CN-{today:yyyyMMdd}";
            var cnCount = await _context.ClothSalesReturns
                .IgnoreQueryFilters()
                .CountAsync(r => r.CreditNoteNumber != null && r.CreditNoteNumber.StartsWith(cnPrefix), cancellationToken);
            creditNoteNumber = $"{cnPrefix}-{(cnCount + 1):D4}";
        }

        var salesReturn = new ClothSalesReturn
        {
            BusinessId = businessId,
            ReturnNumber = returnNumber,
            ReturnDate = today,
            ClothSalesInvoiceId = dto.ClothSalesInvoiceId,
            OriginalInvoiceNumber = dto.OriginalInvoiceNumber,
            CustomerName = string.IsNullOrWhiteSpace(dto.CustomerName) ? "Walk-in Customer" : dto.CustomerName.Trim(),
            CustomerPhone = dto.CustomerPhone?.Trim(),
            ReturnType = isExchange ? "Exchange" : "Return",
            Reason = dto.Reason,
            Remarks = dto.Remarks,
            SettlementMode = dto.SettlementMode,
            CreditNoteNumber = creditNoteNumber,
            HandledBy = _currentUserService.Email ?? "Cashier"
        };

        decimal totalReturnAmount = 0;
        decimal totalExchangeAmount = 0;

        // 3. Process Return Items (coming back into store)
        foreach (var retItem in dto.ReturnItems)
        {
            var variant = await _context.ClothProductVariants
                .Include(v => v.Product)
                .Include(v => v.Size)
                .Include(v => v.Colour)
                .FirstOrDefaultAsync(v => v.Id == retItem.ClothProductVariantId, cancellationToken);

            if (variant == null)
                throw new InvalidOperationException($"Garment variant not found for return: {retItem.ClothProductVariantId}");

            var unitPrice = retItem.UnitPrice > 0 ? retItem.UnitPrice : variant.SellingPrice;
            var lineTotal = unitPrice * retItem.Quantity;
            totalReturnAmount += lineTotal;

            salesReturn.Items.Add(new ClothSalesReturnItem
            {
                ClothSalesReturnId = salesReturn.Id,
                ItemAction = "Return",
                ClothProductVariantId = variant.Id,
                ItemDescription = $"{variant.Product?.Name ?? "Garment"} ({variant.Size?.Name} / {variant.Colour?.Name})",
                Sku = variant.Sku,
                Barcode = variant.Barcode,
                SizeName = variant.Size?.Name ?? string.Empty,
                ColourName = variant.Colour?.Name ?? string.Empty,
                Quantity = retItem.Quantity,
                UnitPrice = unitPrice,
                LineTotal = lineTotal,
                Condition = retItem.Condition
            });

            // If item is fresh/resaleable, replenish showroom inventory!
            if (string.Equals(retItem.Condition, "Fresh / Resaleable", StringComparison.OrdinalIgnoreCase))
            {
                variant.CurrentStock += retItem.Quantity;

                _context.ClothStockLedgers.Add(new ClothStockLedger
                {
                    BusinessId = businessId,
                    ClothProductVariantId = variant.Id,
                    TransactionType = "SalesReturn",
                    QuantityIn = retItem.Quantity,
                    QuantityOut = 0,
                    RunningBalance = variant.CurrentStock,
                    ReferenceNumber = returnNumber,
                    TransactionDate = today,
                    Notes = $"Customer return (Reason: {dto.Reason}). Invoice: {dto.OriginalInvoiceNumber ?? "N/A"}"
                });
            }
            else
            {
                // Defective/Damaged piece: do not replenish active stock, note quarantine
                _context.ClothStockLedgers.Add(new ClothStockLedger
                {
                    BusinessId = businessId,
                    ClothProductVariantId = variant.Id,
                    TransactionType = "DamagedReturnQuarantine",
                    QuantityIn = 0,
                    QuantityOut = 0,
                    RunningBalance = variant.CurrentStock,
                    ReferenceNumber = returnNumber,
                    TransactionDate = today,
                    Notes = $"Damaged garment returned by customer (Quarantined, not added to active showroom stock)."
                });
            }
        }

        // 4. Process Exchange / Replacement Items (if Exchange)
        if (isExchange && dto.ExchangeItems.Any())
        {
            foreach (var excItem in dto.ExchangeItems)
            {
                var variant = await _context.ClothProductVariants
                    .Include(v => v.Product)
                    .Include(v => v.Size)
                    .Include(v => v.Colour)
                    .FirstOrDefaultAsync(v => v.Id == excItem.ClothProductVariantId, cancellationToken);

                if (variant == null)
                    throw new InvalidOperationException($"Garment variant not found for exchange: {excItem.ClothProductVariantId}");

                if (variant.CurrentStock < excItem.Quantity)
                {
                    throw new InvalidOperationException(
                        $"Insufficient showroom stock for replacement garment {variant.Product?.Name} ({variant.Size?.Name}/{variant.Colour?.Name}). Available: {variant.CurrentStock} pcs.");
                }

                var unitPrice = excItem.UnitPrice > 0 ? excItem.UnitPrice : variant.SellingPrice;
                var lineTotal = unitPrice * excItem.Quantity;
                totalExchangeAmount += lineTotal;

                // Deduct replacement stock from showroom
                variant.CurrentStock -= excItem.Quantity;

                salesReturn.Items.Add(new ClothSalesReturnItem
                {
                    ClothSalesReturnId = salesReturn.Id,
                    ItemAction = "Replacement",
                    ClothProductVariantId = variant.Id,
                    ItemDescription = $"{variant.Product?.Name ?? "Garment"} ({variant.Size?.Name} / {variant.Colour?.Name})",
                    Sku = variant.Sku,
                    Barcode = variant.Barcode,
                    SizeName = variant.Size?.Name ?? string.Empty,
                    ColourName = variant.Colour?.Name ?? string.Empty,
                    Quantity = excItem.Quantity,
                    UnitPrice = unitPrice,
                    LineTotal = lineTotal,
                    Condition = "Fresh / Resaleable"
                });

                // Post Stock Ledger entry for replacement item leaving showroom
                _context.ClothStockLedgers.Add(new ClothStockLedger
                {
                    BusinessId = businessId,
                    ClothProductVariantId = variant.Id,
                    TransactionType = "SalesExchange",
                    QuantityIn = 0,
                    QuantityOut = excItem.Quantity,
                    RunningBalance = variant.CurrentStock,
                    ReferenceNumber = returnNumber,
                    TransactionDate = today,
                    Notes = $"Replacement item issued on exchange {returnNumber}"
                });
            }
        }

        salesReturn.TotalReturnAmount = totalReturnAmount;
        salesReturn.TotalExchangeAmount = totalExchangeAmount;
        salesReturn.NetDifference = totalExchangeAmount - totalReturnAmount;

        _context.ClothSalesReturns.Add(salesReturn);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(salesReturn);
    }

    public async Task<ClothSalesReturnSummaryDto> GetReturnsSummaryAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;

        var all = await _context.ClothSalesReturns
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var totalReturns = all.Count(r => r.ReturnType == "Return");
        var totalExchanges = all.Count(r => r.ReturnType == "Exchange");
        var totalReturnedVal = all.Sum(r => r.TotalReturnAmount);
        var totalCreditNotes = all.Count(r => !string.IsNullOrEmpty(r.CreditNoteNumber));

        var todayReturns = all.Count(r => r.ReturnType == "Return" && r.ReturnDate.Date == today);
        var todayExchanges = all.Count(r => r.ReturnType == "Exchange" && r.ReturnDate.Date == today);

        return new ClothSalesReturnSummaryDto
        {
            TotalReturnsCount = totalReturns,
            TotalExchangesCount = totalExchanges,
            TotalReturnedAmount = totalReturnedVal,
            TotalCreditNotesIssued = totalCreditNotes,
            TodayReturnsCount = todayReturns,
            TodayExchangesCount = todayExchanges
        };
    }

    private static ClothSalesReturnDto MapToDto(ClothSalesReturn entity)
    {
        return new ClothSalesReturnDto
        {
            Id = entity.Id,
            ReturnNumber = entity.ReturnNumber,
            ReturnDate = entity.ReturnDate,
            ClothSalesInvoiceId = entity.ClothSalesInvoiceId,
            OriginalInvoiceNumber = entity.OriginalInvoiceNumber,
            CustomerName = entity.CustomerName,
            CustomerPhone = entity.CustomerPhone,
            ReturnType = entity.ReturnType,
            TotalReturnAmount = entity.TotalReturnAmount,
            TotalExchangeAmount = entity.TotalExchangeAmount,
            NetDifference = entity.NetDifference,
            SettlementMode = entity.SettlementMode,
            CreditNoteNumber = entity.CreditNoteNumber,
            Reason = entity.Reason,
            Remarks = entity.Remarks,
            HandledBy = entity.HandledBy,
            Items = entity.Items.Select(i => new ClothSalesReturnItemDto
            {
                Id = i.Id,
                ItemAction = i.ItemAction,
                ClothProductVariantId = i.ClothProductVariantId,
                ItemDescription = i.ItemDescription,
                Sku = i.Sku,
                Barcode = i.Barcode,
                SizeName = i.SizeName,
                ColourName = i.ColourName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                LineTotal = i.LineTotal,
                Condition = i.Condition
            }).ToList()
        };
    }
}
