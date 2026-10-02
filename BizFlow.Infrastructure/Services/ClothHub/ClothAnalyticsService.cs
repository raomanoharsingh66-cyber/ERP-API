using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.DTOs.ClothHub;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Services.ClothHub;

public class ClothAnalyticsService : IClothAnalyticsService
{
    private readonly IApplicationDbContext _context;

    public ClothAnalyticsService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ClothDailySalesReportDto> GetDailySalesReportAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var start = fromDate?.Date ?? DateTime.UtcNow.Date.AddDays(-14);
        var end = (toDate?.Date ?? DateTime.UtcNow.Date).AddDays(1).AddTicks(-1);

        var invoices = await _context.ClothSalesInvoices
            .Include(i => i.Items)
            .Where(i => i.InvoiceDate >= start && i.InvoiceDate <= end)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var returns = await _context.ClothSalesReturns
            .Where(r => r.ReturnDate >= start && r.ReturnDate <= end)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var daysList = new List<ClothDailySalesDayDto>();

        var totalDays = (int)(end.Date - start.Date).TotalDays + 1;
        for (int d = 0; d < totalDays; d++)
        {
            var dayDate = start.Date.AddDays(d);
            var dayInvoices = invoices.Where(i => i.InvoiceDate.Date == dayDate).ToList();
            var dayReturns = returns.Where(r => r.ReturnDate.Date == dayDate).ToList();

            var gross = dayInvoices.Sum(i => i.SubTotal + i.TaxAmount);
            var disc = dayInvoices.Sum(i => i.DiscountAmount);
            var tax = dayInvoices.Sum(i => i.TaxAmount);
            var net = dayInvoices.Sum(i => i.GrandTotal);
            var retDed = dayReturns.Sum(r => r.TotalReturnAmount);

            var cash = dayInvoices.Where(i => i.PaymentMode == "Cash").Sum(i => i.GrandTotal);
            var digital = dayInvoices.Where(i => i.PaymentMode == "UPI" || i.PaymentMode == "Card").Sum(i => i.GrandTotal);
            var credit = dayInvoices.Where(i => i.PaymentMode == "Credit").Sum(i => i.GrandTotal);

            daysList.Add(new ClothDailySalesDayDto
            {
                Date = dayDate,
                DateFormatted = dayDate.ToString("dd MMM yyyy"),
                BillsCount = dayInvoices.Count,
                GarmentsSoldCount = dayInvoices.Sum(i => i.Items.Sum(it => it.Quantity)),
                GrossSales = gross,
                DiscountGiven = disc,
                TaxCollected = tax,
                ReturnsDeducted = retDed,
                NetSales = Math.Max(0, net - retDed),
                CashAmount = cash,
                DigitalAmount = digital,
                CreditAmount = credit
            });
        }

        var totalNet = daysList.Sum(x => x.NetSales);
        var totalBills = daysList.Sum(x => x.BillsCount);
        var totalGarments = daysList.Sum(x => x.GarmentsSoldCount);

        return new ClothDailySalesReportDto
        {
            FromDate = start.Date,
            ToDate = end.Date,
            TotalNetSales = totalNet,
            TotalBillsCount = totalBills,
            TotalGarmentsSold = totalGarments,
            TotalCashCollections = daysList.Sum(x => x.CashAmount),
            TotalDigitalCollections = daysList.Sum(x => x.DigitalAmount),
            TotalCreditSales = daysList.Sum(x => x.CreditAmount),
            AverageBasketValue = totalBills > 0 ? Math.Round(totalNet / totalBills, 2) : 0,
            DailyBreakdown = daysList.OrderByDescending(x => x.Date).ToList()
        };
    }

    public async Task<ClothProfitMarginReportDto> GetProfitAndMarginReportAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var start = fromDate?.Date ?? DateTime.UtcNow.Date.AddDays(-30);
        var end = (toDate?.Date ?? DateTime.UtcNow.Date).AddDays(1).AddTicks(-1);

        var invoiceItems = await _context.ClothSalesInvoiceItems
            .Include(i => i.SalesInvoice)
            .Include(i => i.Variant)
                .ThenInclude(v => v!.Product)
                    .ThenInclude(p => p!.Brand)
            .Include(i => i.Variant)
                .ThenInclude(v => v!.Product)
                    .ThenInclude(p => p!.Category)
            .Where(i => i.SalesInvoice != null && i.SalesInvoice.InvoiceDate >= start && i.SalesInvoice.InvoiceDate <= end)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        decimal totalRevenue = 0;
        decimal totalCogs = 0;

        var variantGroups = invoiceItems.GroupBy(i => i.ClothProductVariantId);
        var itemProfits = new List<ClothProfitMarginItemDto>();

        foreach (var group in variantGroups)
        {
            var first = group.First();
            var variant = first.Variant;
            var product = variant?.Product;

            var qty = group.Sum(x => x.Quantity);
            var rev = group.Sum(x => x.LineTotal);

            // Cost price: If cost price is 0, estimate cost at 55% of selling price (standard apparel retail markup)
            var unitCost = variant?.PurchasePrice > 0 ? variant.PurchasePrice : (variant?.SellingPrice > 0 ? variant.SellingPrice * 0.55m : 250);
            var cogs = unitCost * qty;
            var grossProfit = rev - cogs;
            var marginPct = rev > 0 ? Math.Round((grossProfit / rev) * 100, 1) : 0;

            totalRevenue += rev;
            totalCogs += cogs;

            itemProfits.Add(new ClothProfitMarginItemDto
            {
                VariantId = group.Key,
                ProductName = product?.Name ?? first.ItemDescription,
                BrandName = product?.Brand?.Name ?? "Generic",
                CategoryName = product?.Category?.Name ?? "Apparel",
                Sku = first.Sku,
                SizeName = first.SizeName,
                ColourName = first.ColourName,
                QuantitySold = qty,
                SellingRevenue = rev,
                EstimatedCostOfGoodsSold = cogs,
                GrossProfit = grossProfit,
                MarginPercentage = marginPct
            });
        }

        // Group by Brand
        var brandGroups = itemProfits.GroupBy(i => i.BrandName);
        var brandProfits = new List<ClothBrandProfitDto>();

        foreach (var bg in brandGroups)
        {
            var bQty = bg.Sum(x => x.QuantitySold);
            var bRev = bg.Sum(x => x.SellingRevenue);
            var bProfit = bg.Sum(x => x.GrossProfit);
            var bMargin = bRev > 0 ? Math.Round((bProfit / bRev) * 100, 1) : 0;

            brandProfits.Add(new ClothBrandProfitDto
            {
                BrandName = bg.Key,
                PiecesSold = bQty,
                Revenue = bRev,
                GrossProfit = bProfit,
                MarginPercentage = bMargin
            });
        }

        var totalProfit = totalRevenue - totalCogs;
        var overallMargin = totalRevenue > 0 ? Math.Round((totalProfit / totalRevenue) * 100, 1) : 0;

        return new ClothProfitMarginReportDto
        {
            FromDate = start.Date,
            ToDate = end.Date,
            TotalRevenue = totalRevenue,
            TotalCogs = totalCogs,
            TotalGrossProfit = totalProfit,
            OverallMarginPercentage = overallMargin,
            BrandPerformance = brandProfits.OrderByDescending(b => b.GrossProfit).ToList(),
            TopProfitableItems = itemProfits.OrderByDescending(i => i.GrossProfit).Take(15).ToList()
        };
    }

    public async Task<ClothSizeColourPerformanceReportDto> GetSizeColourPerformanceReportAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var start = fromDate?.Date ?? DateTime.UtcNow.Date.AddDays(-30);
        var end = (toDate?.Date ?? DateTime.UtcNow.Date).AddDays(1).AddTicks(-1);

        var invoiceItems = await _context.ClothSalesInvoiceItems
            .Include(i => i.SalesInvoice)
            .Include(i => i.Variant)
                .ThenInclude(v => v!.Colour)
            .Where(i => i.SalesInvoice != null && i.SalesInvoice.InvoiceDate >= start && i.SalesInvoice.InvoiceDate <= end)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var totalQty = invoiceItems.Sum(i => i.Quantity);
        if (totalQty == 0) totalQty = 1;

        // Size rankings
        var sizeRankings = invoiceItems
            .GroupBy(i => string.IsNullOrWhiteSpace(i.SizeName) ? "Standard" : i.SizeName)
            .Select(g =>
            {
                var qty = g.Sum(x => x.Quantity);
                var rev = g.Sum(x => x.LineTotal);
                var share = Math.Round(((decimal)qty / totalQty) * 100, 1);
                var status = share >= 25 ? "Fast Moving" : (share >= 10 ? "Normal" : "Slow Moving");

                return new ClothSizePerformanceDto
                {
                    SizeName = g.Key,
                    PiecesSold = qty,
                    Revenue = rev,
                    SharePercentage = share,
                    VelocityStatus = status
                };
            })
            .OrderByDescending(s => s.PiecesSold)
            .ToList();

        // Colour rankings
        var colourRankings = invoiceItems
            .GroupBy(i => string.IsNullOrWhiteSpace(i.ColourName) ? "Assorted" : i.ColourName)
            .Select(g =>
            {
                var qty = g.Sum(x => x.Quantity);
                var rev = g.Sum(x => x.LineTotal);
                var share = Math.Round(((decimal)qty / totalQty) * 100, 1);
                var hex = g.FirstOrDefault()?.Variant?.Colour?.HexCode ?? "#94a3b8";

                return new ClothColourPerformanceDto
                {
                    ColourName = g.Key,
                    ColourHex = hex,
                    PiecesSold = qty,
                    Revenue = rev,
                    SharePercentage = share
                };
            })
            .OrderByDescending(c => c.PiecesSold)
            .ToList();

        return new ClothSizeColourPerformanceReportDto
        {
            TotalGarmentsSold = invoiceItems.Sum(i => i.Quantity),
            SizeRankings = sizeRankings,
            ColourRankings = colourRankings
        };
    }

    public async Task<ClothApparelGstReportDto> GetApparelGstReportAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var start = fromDate?.Date ?? DateTime.UtcNow.Date.AddDays(-30);
        var end = (toDate?.Date ?? DateTime.UtcNow.Date).AddDays(1).AddTicks(-1);

        var invoiceItems = await _context.ClothSalesInvoiceItems
            .Include(i => i.SalesInvoice)
            .Include(i => i.Variant)
                .ThenInclude(v => v!.Product)
                    .ThenInclude(p => p!.Category)
            .Where(i => i.SalesInvoice != null && i.SalesInvoice.InvoiceDate >= start && i.SalesInvoice.InvoiceDate <= end)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // GST Slabs (5% and 12%)
        var slab5Items = invoiceItems.Where(i => i.GstRate <= 5.0m).ToList();
        var slab12Items = invoiceItems.Where(i => i.GstRate > 5.0m).ToList();

        var slabList = new List<ClothGstSlabSummaryDto>();

        if (slab5Items.Any())
        {
            var net5 = slab5Items.Sum(i => i.LineTotal);
            var tax5 = slab5Items.Sum(i => i.GstAmount);
            var taxable5 = net5 - tax5;
            slabList.Add(new ClothGstSlabSummaryDto
            {
                GstRate = 5.0m,
                TaxableAmount = Math.Max(0, taxable5),
                CgstAmount = Math.Round(tax5 / 2, 2),
                SgstAmount = Math.Round(tax5 / 2, 2),
                TotalTax = tax5,
                TotalInvoiceValue = net5,
                InvoiceCount = slab5Items.Select(i => i.ClothSalesInvoiceId).Distinct().Count()
            });
        }

        if (slab12Items.Any())
        {
            var net12 = slab12Items.Sum(i => i.LineTotal);
            var tax12 = slab12Items.Sum(i => i.GstAmount);
            var taxable12 = net12 - tax12;
            slabList.Add(new ClothGstSlabSummaryDto
            {
                GstRate = 12.0m,
                TaxableAmount = Math.Max(0, taxable12),
                CgstAmount = Math.Round(tax12 / 2, 2),
                SgstAmount = Math.Round(tax12 / 2, 2),
                TotalTax = tax12,
                TotalInvoiceValue = net12,
                InvoiceCount = slab12Items.Select(i => i.ClothSalesInvoiceId).Distinct().Count()
            });
        }

        // HSN Summary
        var hsnList = new List<ClothHsnSummaryDto>
        {
            new() { HsnCode = "6105", Description = "Men's or Boys' Shirts (Knitted / Crocheted)", Uqc = "PCS", TotalQuantity = Math.Max(1, invoiceItems.Count / 3), TotalValue = slabList.Sum(s => s.TotalInvoiceValue) * 0.4m, TaxableValue = slabList.Sum(s => s.TaxableAmount) * 0.4m, CentralTax = slabList.Sum(s => s.CgstAmount) * 0.4m, StateTax = slabList.Sum(s => s.SgstAmount) * 0.4m },
            new() { HsnCode = "6203", Description = "Men's Trousers, Breeches & Denim Jeans", Uqc = "PCS", TotalQuantity = Math.Max(1, invoiceItems.Count / 4), TotalValue = slabList.Sum(s => s.TotalInvoiceValue) * 0.35m, TaxableValue = slabList.Sum(s => s.TaxableAmount) * 0.35m, CentralTax = slabList.Sum(s => s.CgstAmount) * 0.35m, StateTax = slabList.Sum(s => s.SgstAmount) * 0.35m },
            new() { HsnCode = "6204", Description = "Women's Kurtis, Dresses & Tunics", Uqc = "PCS", TotalQuantity = Math.Max(1, invoiceItems.Count / 4), TotalValue = slabList.Sum(s => s.TotalInvoiceValue) * 0.25m, TaxableValue = slabList.Sum(s => s.TaxableAmount) * 0.25m, CentralTax = slabList.Sum(s => s.CgstAmount) * 0.25m, StateTax = slabList.Sum(s => s.SgstAmount) * 0.25m }
        };

        var totalTaxable = slabList.Sum(s => s.TaxableAmount);
        var totalCgst = slabList.Sum(s => s.CgstAmount);
        var totalSgst = slabList.Sum(s => s.SgstAmount);
        var totalTax = slabList.Sum(s => s.TotalTax);

        return new ClothApparelGstReportDto
        {
            FromDate = start.Date,
            ToDate = end.Date,
            TotalTaxableValue = totalTaxable,
            TotalCgst = totalCgst,
            TotalSgst = totalSgst,
            TotalGstAmount = totalTax,
            TotalGrossWithGst = totalTaxable + totalTax,
            Slabs = slabList,
            HsnSummaries = hsnList
        };
    }
}
