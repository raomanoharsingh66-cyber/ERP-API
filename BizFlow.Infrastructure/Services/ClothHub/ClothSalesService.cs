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

public class ClothSalesService : IClothSalesService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ClothSalesService(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    private Guid GetEffectiveBusinessId()
    {
        return _currentUserService.BusinessId ?? Guid.Parse("22222222-2222-2222-2222-222222222222");
    }

    // --- 1. FAST POS LOOKUP & SEARCH ---
    public async Task<ClothPosLookupItemDto?> LookupVariantByBarcodeOrSkuAsync(string term, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(term))
            return null;

        var clean = term.Trim().ToUpper();

        var variant = await _context.ClothProductVariants
            .AsNoTracking()
            .Include(v => v.Product)
                .ThenInclude(p => p!.Brand)
            .Include(v => v.Product)
                .ThenInclude(p => p!.Category)
            .Include(v => v.Size)
            .Include(v => v.Colour)
            .FirstOrDefaultAsync(v => v.Barcode == clean || v.Sku.ToUpper() == clean, cancellationToken);

        if (variant == null)
            return null;

        return new ClothPosLookupItemDto
        {
            VariantId = variant.Id,
            ProductId = variant.ClothProductId,
            ProductName = variant.Product?.Name ?? "Garment Article",
            BrandName = variant.Product?.Brand?.Name ?? "",
            CategoryName = variant.Product?.Category?.Name ?? "",
            Sku = variant.Sku,
            Barcode = variant.Barcode,
            SizeName = variant.Size?.Name ?? "",
            ColourName = variant.Colour?.Name ?? "",
            ColourHex = variant.Colour?.HexCode ?? "#000000",
            Mrp = variant.Mrp,
            SellingPrice = variant.SellingPrice,
            CurrentStock = variant.CurrentStock,
            GstRate = variant.Product?.GstRate ?? 5.0m
        };
    }

    public async Task<List<ClothPosLookupItemDto>> SearchPosItemsAsync(string? query = null, Guid? categoryId = null, CancellationToken cancellationToken = default)
    {
        var dbQuery = _context.ClothProductVariants
            .AsNoTracking()
            .Include(v => v.Product)
                .ThenInclude(p => p!.Brand)
            .Include(v => v.Product)
                .ThenInclude(p => p!.Category)
            .Include(v => v.Size)
            .Include(v => v.Colour)
            .AsQueryable();

        if (categoryId.HasValue)
        {
            dbQuery = dbQuery.Where(v => v.Product != null && v.Product.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim().ToLower();
            dbQuery = dbQuery.Where(v =>
                v.Sku.ToLower().Contains(q) ||
                v.Barcode.ToLower().Contains(q) ||
                (v.Product != null && v.Product.Name.ToLower().Contains(q)) ||
                (v.Size != null && v.Size.Name.ToLower().Contains(q)) ||
                (v.Colour != null && v.Colour.Name.ToLower().Contains(q))
            );
        }

        var results = await dbQuery
            .OrderByDescending(v => v.CurrentStock)
            .Take(40)
            .ToListAsync(cancellationToken);

        return results.Select(v => new ClothPosLookupItemDto
        {
            VariantId = v.Id,
            ProductId = v.ClothProductId,
            ProductName = v.Product?.Name ?? "Garment Article",
            BrandName = v.Product?.Brand?.Name ?? "",
            CategoryName = v.Product?.Category?.Name ?? "",
            Sku = v.Sku,
            Barcode = v.Barcode,
            SizeName = v.Size?.Name ?? "",
            ColourName = v.Colour?.Name ?? "",
            ColourHex = v.Colour?.HexCode ?? "#000000",
            Mrp = v.Mrp,
            SellingPrice = v.SellingPrice,
            CurrentStock = v.CurrentStock,
            GstRate = v.Product?.GstRate ?? 5.0m
        }).ToList();
    }

    // --- 2. SALES INVOICES & BILLING ---
    public async Task<List<ClothSalesInvoiceDto>> GetSalesInvoicesAsync(DateTime? fromDate = null, DateTime? toDate = null, string? search = null, CancellationToken cancellationToken = default)
    {
        var dbQuery = _context.ClothSalesInvoices
            .AsNoTracking()
            .Include(i => i.Items)
                .ThenInclude(it => it.Variant)
            .AsQueryable();

        if (fromDate.HasValue)
            dbQuery = dbQuery.Where(i => i.InvoiceDate >= fromDate.Value);

        if (toDate.HasValue)
            dbQuery = dbQuery.Where(i => i.InvoiceDate <= toDate.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            dbQuery = dbQuery.Where(i =>
                i.InvoiceNumber.ToLower().Contains(s) ||
                i.CustomerName.ToLower().Contains(s) ||
                (i.CustomerPhone != null && i.CustomerPhone.Contains(s))
            );
        }

        var invoices = await dbQuery
            .OrderByDescending(i => i.InvoiceDate)
            .Take(100)
            .ToListAsync(cancellationToken);

        return invoices.Select(i => MapToInvoiceDto(i)).ToList();
    }

    public async Task<ClothSalesInvoiceDto?> GetSalesInvoiceByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invoice = await _context.ClothSalesInvoices
            .AsNoTracking()
            .Include(i => i.Items)
                .ThenInclude(it => it.Variant)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        return invoice != null ? MapToInvoiceDto(invoice) : null;
    }

    public async Task<ClothSalesInvoiceDto> CreateSalesInvoiceAsync(CreateClothSalesInvoiceDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.Items == null || dto.Items.Count == 0)
            throw new InvalidOperationException("At least 1 garment item must be added to the cart.");

        var businessId = GetEffectiveBusinessId();
        var now = DateTime.UtcNow;

        var count = await _context.ClothSalesInvoices.CountAsync(cancellationToken);
        var invoiceNumber = $"POS-{now:yyyyMMdd}-{(count + 1):D4}";

        var invoice = new ClothSalesInvoice
        {
            BusinessId = businessId,
            InvoiceNumber = invoiceNumber,
            InvoiceDate = now,
            CustomerName = !string.IsNullOrWhiteSpace(dto.CustomerName) ? dto.CustomerName.Trim() : "Walk-in Customer",
            CustomerPhone = dto.CustomerPhone?.Trim(),
            CustomerGstin = dto.CustomerGstin?.Trim().ToUpper(),
            DiscountAmount = dto.DiscountAmount,
            PaymentMode = dto.PaymentMode,
            PaymentBreakdownJson = dto.PaymentBreakdownJson,
            Status = "Completed",
            CashierName = _currentUserService.Email ?? "Cashier",
            Notes = dto.Notes
        };

        decimal subTotal = 0;
        decimal taxAmount = 0;

        foreach (var itemDto in dto.Items)
        {
            var variant = await _context.ClothProductVariants
                .Include(v => v.Product)
                .Include(v => v.Size)
                .Include(v => v.Colour)
                .FirstOrDefaultAsync(v => v.Id == itemDto.ClothProductVariantId, cancellationToken);

            if (variant == null)
                throw new KeyNotFoundException($"Garment variant not found: {itemDto.ClothProductVariantId}");

            var gstRate = variant.Product?.GstRate ?? 5.0m;
            var lineGross = itemDto.Quantity * variant.SellingPrice;
            var lineNet = Math.Max(0, lineGross - itemDto.DiscountAmount);
            var lineTax = lineNet * (gstRate / (100m + gstRate)); // Inclusive GST calculation standard in apparel retail
            var lineSub = lineNet - lineTax;

            subTotal += lineSub;
            taxAmount += lineTax;

            var item = new ClothSalesInvoiceItem
            {
                ClothProductVariantId = variant.Id,
                ItemDescription = $"{variant.Product?.Name ?? "Garment"} ({variant.Size?.Name ?? ""}/{variant.Colour?.Name ?? ""})",
                Sku = variant.Sku,
                Barcode = variant.Barcode,
                SizeName = variant.Size?.Name ?? "",
                ColourName = variant.Colour?.Name ?? "",
                Quantity = itemDto.Quantity,
                Mrp = variant.Mrp,
                UnitSellingPrice = variant.SellingPrice,
                DiscountAmount = itemDto.DiscountAmount,
                GstRate = gstRate,
                GstAmount = lineTax,
                LineTotal = lineNet
            };

            invoice.Items.Add(item);

            // Deduct stock balance
            variant.CurrentStock = Math.Max(0, variant.CurrentStock - itemDto.Quantity);

            // Add immutable ledger entry
            _context.ClothStockLedgers.Add(new ClothStockLedger
            {
                BusinessId = businessId,
                ClothProductVariantId = variant.Id,
                TransactionDate = now,
                TransactionType = "SalePos",
                ReferenceNumber = invoiceNumber,
                QuantityIn = 0,
                QuantityOut = itemDto.Quantity,
                RunningBalance = variant.CurrentStock,
                UnitCost = variant.PurchasePrice,
                Notes = $"Retail POS Sale #{invoiceNumber} ({dto.PaymentMode})"
            });
        }

        var grandTotal = Math.Round(subTotal + taxAmount, 2);
        invoice.SubTotal = subTotal;
        invoice.TaxAmount = taxAmount;
        invoice.GrandTotal = grandTotal;
        invoice.PaidAmount = dto.PaidAmount > 0 ? dto.PaidAmount : grandTotal;
        invoice.ChangeReturned = Math.Max(0, invoice.PaidAmount - grandTotal);

        _context.ClothSalesInvoices.Add(invoice);
        await _context.SaveChangesAsync(cancellationToken);

        return (await GetSalesInvoicesAsync(now.Date, now.Date.AddDays(1), invoiceNumber, cancellationToken)).First();
    }

    // --- 3. SALES SUMMARY ---
    public async Task<ClothSalesSummaryDto> GetSalesSummaryAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var todayInvoices = await _context.ClothSalesInvoices
            .AsNoTracking()
            .Include(i => i.Items)
            .Where(i => i.InvoiceDate >= today && i.InvoiceDate < tomorrow)
            .ToListAsync(cancellationToken);

        var lifetimeInvoices = await _context.ClothSalesInvoices
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return new ClothSalesSummaryDto
        {
            TodaySalesTotal = todayInvoices.Sum(i => i.GrandTotal),
            TodayBillsCount = todayInvoices.Count,
            TodayGarmentsSold = todayInvoices.Sum(i => i.Items.Sum(it => it.Quantity)),
            TodayCashSales = todayInvoices.Where(i => i.PaymentMode == "Cash").Sum(i => i.GrandTotal),
            TodayUpiSales = todayInvoices.Where(i => i.PaymentMode == "UPI").Sum(i => i.GrandTotal),
            TodayCardSales = todayInvoices.Where(i => i.PaymentMode == "Card").Sum(i => i.GrandTotal),
            LifetimeSalesTotal = lifetimeInvoices.Sum(i => i.GrandTotal),
            LifetimeBillsCount = lifetimeInvoices.Count
        };
    }

    private static ClothSalesInvoiceDto MapToInvoiceDto(ClothSalesInvoice i)
    {
        return new ClothSalesInvoiceDto
        {
            Id = i.Id,
            InvoiceNumber = i.InvoiceNumber,
            InvoiceDate = i.InvoiceDate,
            CustomerName = i.CustomerName,
            CustomerPhone = i.CustomerPhone,
            CustomerGstin = i.CustomerGstin,
            SubTotal = i.SubTotal,
            TaxAmount = i.TaxAmount,
            DiscountAmount = i.DiscountAmount,
            RoundOff = i.RoundOff,
            GrandTotal = i.GrandTotal,
            PaidAmount = i.PaidAmount,
            ChangeReturned = i.ChangeReturned,
            PaymentMode = i.PaymentMode,
            PaymentBreakdownJson = i.PaymentBreakdownJson,
            Status = i.Status,
            CashierName = i.CashierName,
            Notes = i.Notes,
            TotalPieces = i.Items.Sum(it => it.Quantity),
            Items = i.Items.Select(it => new ClothSalesInvoiceItemDto
            {
                Id = it.Id,
                ClothProductVariantId = it.ClothProductVariantId,
                ItemDescription = it.ItemDescription,
                Sku = it.Sku,
                Barcode = it.Barcode,
                SizeName = it.SizeName,
                ColourName = it.ColourName,
                Quantity = it.Quantity,
                Mrp = it.Mrp,
                UnitSellingPrice = it.UnitSellingPrice,
                DiscountAmount = it.DiscountAmount,
                GstRate = it.GstRate,
                GstAmount = it.GstAmount,
                LineTotal = it.LineTotal,
                IsExchangedOrReturned = it.IsExchangedOrReturned
            }).ToList()
        };
    }
}
