using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.DTOs.AiAssistant;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BizFlow.Infrastructure.Services;

public class AiAssistantService : IAiAssistantService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AiAssistantService> _logger;

    public AiAssistantService(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IConfiguration configuration,
        ILogger<AiAssistantService> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _configuration = configuration;
        _logger = logger;
    }

    private Guid RequireBusinessId()
    {
        if (_currentUserService.BusinessId != null)
        {
            return _currentUserService.BusinessId.Value;
        }

        var defaultBusiness = _context.Businesses.OrderBy(b => b.CreatedOn).FirstOrDefault();
        if (defaultBusiness != null)
        {
            return defaultBusiness.Id;
        }

        throw new BusinessRuleException("A business context is required to query AI Assistant insights.");
    }

    public async Task<SmartPurchaseDashboardDto> GetSmartPurchaseDashboardAsync(CancellationToken cancellationToken = default)
    {
        var reorders = await GetSmartReordersAsync(cancellationToken);
        var demandTrends = await GetDemandTrendsAsync(cancellationToken);
        var supplierPerformances = await GetSupplierPerformancesAsync(cancellationToken);

        var costTrendProducts = reorders
            .Where(r => r.PriceChangePercentage > 3m || (r.LastPurchasePrice > r.AverageHistoricalPrice && r.AverageHistoricalPrice > 0))
            .Select(r => new CostTrendProductDto
            {
                ProductId = r.ProductId,
                ProductName = r.ProductName,
                SKU = r.SKU,
                CurrentPurchasePrice = r.CurrentSupplierPrice > 0 ? r.CurrentSupplierPrice : r.LastPurchasePrice,
                PreviousPurchasePrice = r.AverageHistoricalPrice > 0 ? r.AverageHistoricalPrice : r.LastPurchasePrice,
                HistoricalAveragePrice = r.AverageHistoricalPrice,
                PriceIncreasePercentage = r.PriceChangePercentage,
                PreferredSupplierName = r.PreferredSupplierName,
                CostAssessment = r.PriceChangePercentage > 0
                    ? $"Unit purchase cost increased by {r.PriceChangePercentage:0.1}% over historical baseline."
                    : "Stable purchase cost."
            })
            .OrderByDescending(c => c.PriceIncreasePercentage)
            .Take(10)
            .ToList();

        var summary = new InsightSummaryKpisDto
        {
            CriticalStockCount = reorders.Count(r => r.StockHealthStatus == "Critical"),
            LowStockCount = reorders.Count(r => r.StockHealthStatus == "Low"),
            HealthyStockCount = reorders.Count(r => r.StockHealthStatus == "Healthy"),
            OverstockedCount = reorders.Count(r => r.StockHealthStatus == "Overstocked"),
            IncreasingDemandCount = demandTrends.Count(d => d.TrendDirection == "Increasing" || d.IsSpike),
            DecreasingDemandCount = demandTrends.Count(d => d.TrendDirection == "Decreasing" || d.IsDrop),
            CostIncreasingCount = costTrendProducts.Count,
            DelayedSuppliersCount = supplierPerformances.Count(s => s.HasLongDeliveryTimes),
            RecommendedPurchasesCount = reorders.Count(r => r.RecommendedPurchaseQuantity > 0)
        };

        var totalEstimatedReorderCost = reorders
            .Where(r => r.RecommendedPurchaseQuantity > 0)
            .Sum(r => r.RecommendedPurchaseQuantity * (r.CurrentSupplierPrice > 0 ? r.CurrentSupplierPrice : r.LastPurchasePrice));

        return new SmartPurchaseDashboardDto
        {
            SummaryKpis = summary,
            ReorderRecommendations = reorders,
            DemandTrends = demandTrends,
            SupplierPerformances = supplierPerformances,
            CostTrendProducts = costTrendProducts,
            EstimatedTotalReorderCost = Math.Round(totalEstimatedReorderCost, 2),
            GeneratedAt = _dateTimeProvider.UtcDateTime
        };
    }

    public async Task<List<SmartReorderRecommendationDto>> GetSmartReordersAsync(CancellationToken cancellationToken = default)
    {
        var businessId = RequireBusinessId();
        var now = _dateTimeProvider.UtcDateTime;
        var date7dAgo = now.AddDays(-7);
        var date30dAgo = now.AddDays(-30);
        var date60dAgo = now.AddDays(-60);
        var date90dAgo = now.AddDays(-90);

        // 1. Fetch Products
        var products = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.UnitOfMeasure)
            .Where(p => p.BusinessId == businessId && p.IsActive)
            .ToListAsync(cancellationToken);

        // 2. Fetch Inventory Stocks
        var stocks = await _context.InventoryStocks
            .AsNoTracking()
            .Include(s => s.Warehouse)
            .Where(s => s.BusinessId == businessId)
            .ToListAsync(cancellationToken);

        // 3. Fetch Sales Invoices for consumption velocity
        var salesInvoiceItems = await _context.SalesInvoiceItems
            .AsNoTracking()
            .Include(sii => sii.SalesInvoice)
            .Where(sii => sii.SalesInvoice != null &&
                          sii.SalesInvoice.BusinessId == businessId &&
                          sii.SalesInvoice.Status != InvoiceStatus.Cancelled &&
                          sii.SalesInvoice.InvoiceDate >= date90dAgo)
            .ToListAsync(cancellationToken);

        // 4. Fetch Open Sales Orders (pending outgoing demand)
        var openSalesOrderItems = await _context.SalesOrderItems
            .AsNoTracking()
            .Include(soi => soi.SalesOrder)
            .Where(soi => soi.SalesOrder != null &&
                          soi.SalesOrder.BusinessId == businessId &&
                          (soi.SalesOrder.Status == OrderStatus.Draft ||
                           soi.SalesOrder.Status == OrderStatus.Confirmed))
            .ToListAsync(cancellationToken);

        // 5. Fetch Open Purchase Orders (incoming replenishment)
        var openPOItems = await _context.PurchaseOrderItems
            .AsNoTracking()
            .Include(poi => poi.PurchaseOrder)
            .Where(poi => poi.PurchaseOrder != null &&
                          poi.PurchaseOrder.BusinessId == businessId &&
                          (poi.PurchaseOrder.Status == PurchaseOrderStatus.Draft ||
                           poi.PurchaseOrder.Status == PurchaseOrderStatus.Approved ||
                           poi.PurchaseOrder.Status == PurchaseOrderStatus.PartiallyReceived))
            .ToListAsync(cancellationToken);

        // 6. Fetch Historical Purchase Order Items for pricing and supplier analysis
        var historicalPOItems = await _context.PurchaseOrderItems
            .AsNoTracking()
            .Include(poi => poi.PurchaseOrder)
                .ThenInclude(po => po!.Supplier)
            .Where(poi => poi.PurchaseOrder != null &&
                          poi.PurchaseOrder.BusinessId == businessId &&
                          poi.PurchaseOrder.Status != PurchaseOrderStatus.Cancelled)
            .ToListAsync(cancellationToken);

        // 7. Fetch Goods Receipt Notes for supplier lead time calculations
        var grns = await _context.GoodsReceiptNotes
            .AsNoTracking()
            .Include(g => g.PurchaseOrder)
            .Where(g => g.BusinessId == businessId && g.PurchaseOrderId != null)
            .ToListAsync(cancellationToken);

        var recommendations = new List<SmartReorderRecommendationDto>();

        foreach (var product in products)
        {
            var pStocks = stocks.Where(s => s.ProductId == product.Id).ToList();
            var currentStock = pStocks.Sum(s => s.QuantityOnHand);
            var reservedStock = pStocks.Sum(s => s.QuantityReserved);
            var availableStock = Math.Max(0m, currentStock - reservedStock);

            // Warehouse breakdown
            var whBreakdown = pStocks.Select(s => new WarehouseStockBreakdownDto
            {
                WarehouseId = s.WarehouseId,
                WarehouseName = s.Warehouse?.Name ?? "Main Warehouse",
                QuantityOnHand = s.QuantityOnHand,
                QuantityReserved = s.QuantityReserved,
                QuantityAvailable = Math.Max(0m, s.QuantityOnHand - s.QuantityReserved)
            }).ToList();

            // Sales consumption metrics
            var pSalesItems = salesInvoiceItems.Where(i => i.ProductId == product.Id).ToList();
            var salesLast7d = pSalesItems.Where(i => i.SalesInvoice!.InvoiceDate >= date7dAgo).Sum(i => i.Quantity);
            var salesLast30d = pSalesItems.Where(i => i.SalesInvoice!.InvoiceDate >= date30dAgo).Sum(i => i.Quantity);
            var salesPrev30d = pSalesItems.Where(i => i.SalesInvoice!.InvoiceDate >= date60dAgo && i.SalesInvoice.InvoiceDate < date30dAgo).Sum(i => i.Quantity);
            var salesLast90d = pSalesItems.Sum(i => i.Quantity);

            // Average daily sales calculation: prioritize 90d baseline, then 30d, then 7d
            decimal avgDailySales;
            string dataQuality;
            bool hasSufficientData;

            if (salesLast90d > 0)
            {
                avgDailySales = Math.Round(salesLast90d / 90m, 2);
                dataQuality = "High";
                hasSufficientData = true;
            }
            else if (salesLast30d > 0)
            {
                avgDailySales = Math.Round(salesLast30d / 30m, 2);
                dataQuality = "Limited";
                hasSufficientData = true;
            }
            else if (salesLast7d > 0)
            {
                avgDailySales = Math.Round(salesLast7d / 7m, 2);
                dataQuality = "Limited";
                hasSufficientData = true;
            }
            else
            {
                avgDailySales = 0m;
                dataQuality = "Insufficient";
                hasSufficientData = false;
            }

            var avgMonthlySales = Math.Round(avgDailySales * 30m, 1);
            var salesTrendPct = salesPrev30d > 0
                ? Math.Round(((salesLast30d - salesPrev30d) / salesPrev30d) * 100m, 1)
                : (salesLast30d > 0 ? 100m : 0m);

            // Pending Sales Orders & Incoming POs
            var pendingSalesQty = openSalesOrderItems
                .Where(soi => soi.ProductId == product.Id)
                .Sum(soi => Math.Max(0m, soi.Quantity));

            var incomingStock = openPOItems
                .Where(poi => poi.ProductId == product.Id)
                .Sum(poi => Math.Max(0m, poi.OrderedQuantity - poi.ReceivedQuantity));

            // Historical PO items for pricing & supplier lead time
            var pHistPOs = historicalPOItems
                .Where(poi => poi.ProductId == product.Id)
                .OrderByDescending(poi => poi.PurchaseOrder!.OrderDate)
                .ToList();

            var lastPurchasePrice = pHistPOs.FirstOrDefault()?.UnitPrice ?? product.PurchasePrice;
            var avgHistoricalPrice = pHistPOs.Any()
                ? Math.Round(pHistPOs.Average(poi => poi.UnitPrice), 2)
                : product.PurchasePrice;

            var currentSupplierPrice = lastPurchasePrice;
            var priceChangePct = avgHistoricalPrice > 0
                ? Math.Round(((currentSupplierPrice - avgHistoricalPrice) / avgHistoricalPrice) * 100m, 1)
                : 0m;

            // Preferred Supplier calculation
            var topSupplierGroup = pHistPOs
                .GroupBy(poi => poi.PurchaseOrder!.Supplier)
                .Where(g => g.Key != null)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            Guid? preferredSupplierId = topSupplierGroup?.Key?.Id;
            string preferredSupplierName = topSupplierGroup?.Key?.Name ?? "Primary Vendor";
            string preferredSupplierCode = topSupplierGroup?.Key?.SupplierCode ?? string.Empty;

            // Supplier Lead Time (derived from actual GRN delivery timelines or default 7 days)
            int leadTimeDays = 7;
            var pGRNs = grns.Where(g => pHistPOs.Any(po => po.PurchaseOrderId == g.PurchaseOrderId)).ToList();
            if (pGRNs.Any())
            {
                var deliverySpans = pGRNs
                    .Where(g => g.PurchaseOrder != null && g.ReceiptDate >= g.PurchaseOrder.OrderDate)
                    .Select(g => (g.ReceiptDate - g.PurchaseOrder!.OrderDate).TotalDays)
                    .Where(d => d >= 1 && d <= 60)
                    .ToList();

                if (deliverySpans.Any())
                {
                    leadTimeDays = (int)Math.Max(3, Math.Round(deliverySpans.Average()));
                }
            }

            // Deterministic Reorder Thresholds
            var reorderLevel = product.MinStockLevel > 0
                ? product.MinStockLevel
                : Math.Max(10m, Math.Round(avgMonthlySales * 0.25m, 0));

            var maxStockLevel = product.MaxStockLevel > 0 ? product.MaxStockLevel : Math.Max(reorderLevel * 4m, 1000m);

            // Safety Stock: Expected Demand during safety buffer (5 to 7 days)
            var safetyStock = Math.Round(avgDailySales * 7m, 0);
            if (safetyStock == 0 && reorderLevel > 0)
            {
                safetyStock = Math.Round(reorderLevel * 0.4m, 0);
            }

            // Expected Demand During Lead Time
            var expectedDemandDuringLeadTime = Math.Round(avgDailySales * leadTimeDays, 0);

            // Smart Purchase Quantity Formula:
            // Recommended Quantity = Expected Demand During Lead Time + Safety Stock - Available Stock - Incoming Stock
            var grossRequired = expectedDemandDuringLeadTime + safetyStock + pendingSalesQty;
            var netAvailable = availableStock + incomingStock;
            var rawRecommendedQty = grossRequired - netAvailable;

            decimal recommendedPurchaseQty = 0m;
            if (rawRecommendedQty > 0)
            {
                recommendedPurchaseQty = Math.Round(rawRecommendedQty, 0);

                // Ensure it does not recklessly exceed max stock level
                if (availableStock + incomingStock + recommendedPurchaseQty > maxStockLevel)
                {
                    recommendedPurchaseQty = Math.Max(0m, maxStockLevel - (availableStock + incomingStock));
                }
            }

            // Estimated Days until Stockout
            decimal estimatedDaysUntilStockout;
            if (avgDailySales > 0)
            {
                estimatedDaysUntilStockout = Math.Round(availableStock / avgDailySales, 1);
            }
            else
            {
                estimatedDaysUntilStockout = availableStock > 0 ? 999m : 0m;
            }

            // Stock Health Status
            string healthStatus;
            if (availableStock == 0 || estimatedDaysUntilStockout <= 3)
            {
                healthStatus = "Critical";
            }
            else if (availableStock <= reorderLevel || estimatedDaysUntilStockout <= leadTimeDays + 2)
            {
                healthStatus = "Low";
            }
            else if (maxStockLevel > 0 && availableStock >= maxStockLevel * 1.1m)
            {
                healthStatus = "Overstocked";
            }
            else
            {
                healthStatus = "Healthy";
            }

            // AI Recommendation & Reason Text Generation (Transparent & Grounded)
            string aiRecommendation;
            string reason;

            var uom = !string.IsNullOrWhiteSpace(product.UnitOfMeasure?.Code)
                ? product.UnitOfMeasure.Code
                : (!string.IsNullOrWhiteSpace(product.UnitOfMeasure?.Name) ? product.UnitOfMeasure.Name : "Units");

            if (!hasSufficientData)
            {
                aiRecommendation = availableStock <= reorderLevel ? "Review Minimum Stock Level" : "Monitor Stock";
                reason = "There is not enough historical data to provide a reliable demand recommendation. Recommendations are based solely on static minimum stock thresholds.";
                dataQuality = "Insufficient";
            }
            else if (healthStatus == "Critical")
            {
                aiRecommendation = "Purchase Urgently";
                reason = $"Stock is critical ({availableStock} {uom}) with only ~{estimatedDaysUntilStockout:0} days remaining. Expected demand during supplier lead time ({leadTimeDays} days) is {expectedDemandDuringLeadTime} {uom} against incoming stock of {incomingStock} {uom}.";
            }
            else if (healthStatus == "Low")
            {
                aiRecommendation = "Consider purchasing";
                reason = $"Stock ({availableStock} {uom}) is below reorder level ({reorderLevel} {uom}). Recent 30-day consumption is {salesLast30d} {uom} and current consumption trend ({salesTrendPct:+0.0;-0.0;0}%) indicates available stock may not be sufficient for expected demand during supplier lead time ({leadTimeDays} days).";
            }
            else if (healthStatus == "Overstocked")
            {
                aiRecommendation = "Hold Purchases";
                reason = $"Current stock ({availableStock} {uom}) exceeds maximum stock level ({maxStockLevel} {uom}). Daily sales rate of {avgDailySales:0.##} {uom}/day indicates sufficient inventory for ~{estimatedDaysUntilStockout:0} days.";
            }
            else
            {
                aiRecommendation = "Stock Healthy";
                reason = $"Current inventory of {availableStock} {uom} is healthy, providing ~{estimatedDaysUntilStockout:0} days of coverage based on active consumption velocity.";
            }

            // Explanation Details Breakdown
            var explanation = new RecommendationExplanationDto
            {
                FormulaUsed = "Recommended Quantity = Expected Demand During Lead Time + Safety Stock - Available Stock - Incoming Stock",
                ExpectedDemandFormula = $"{avgDailySales:0.##} (Avg Daily Sales) × {leadTimeDays} (Lead Time Days) = {expectedDemandDuringLeadTime} {uom}",
                SafetyStockFormula = $"{avgDailySales:0.##} (Avg Daily Sales) × 7 (Safety Buffer Days) = {safetyStock} {uom}",
                AvailableStockValue = availableStock,
                IncomingStockValue = incomingStock,
                NetShortage = Math.Max(0m, rawRecommendedQty),
                HistoricalPeriodEvaluated = "Last 90 days sales velocity & 12 months purchase invoices",
                Assumptions = new List<string>
                {
                    $"Supplier lead time estimated at {leadTimeDays} days based on historical fulfillment.",
                    $"Safety buffer configured at 7 days of daily sales velocity.",
                    $"Pending sales orders accounted: {pendingSalesQty} {uom}."
                },
                DataPointsConsidered = new List<string>
                {
                    $"Current Physical Stock: {currentStock} {uom}",
                    $"Reserved Stock: {reservedStock} {uom}",
                    $"Available Stock: {availableStock} {uom}",
                    $"Incoming Purchase Orders: {incomingStock} {uom}",
                    $"Average Daily Consumption: {avgDailySales:0.##} {uom}/day",
                    $"Reorder Threshold: {reorderLevel} {uom}"
                },
                RecommendedAction = recommendedPurchaseQty > 0
                    ? $"Raise a purchase request for {recommendedPurchaseQty} {uom} with preferred vendor {preferredSupplierName}."
                    : "No immediate purchase required.",
                DataQualityNote = dataQuality == "High"
                    ? "Calculated from comprehensive multi-month sales and delivery records."
                    : (dataQuality == "Limited"
                        ? "Calculated from limited recent transaction records."
                        : "Insufficient transactional history. Purely threshold-guided.")
            };

            recommendations.Add(new SmartReorderRecommendationDto
            {
                ProductId = product.Id,
                ProductName = product.Name,
                SKU = product.SKU,
                CategoryName = product.Category?.Name ?? "General",
                UnitOfMeasure = uom,
                CurrentStock = currentStock,
                ReservedStock = reservedStock,
                AvailableStock = availableStock,
                ReorderLevel = reorderLevel,
                MaxStockLevel = maxStockLevel,
                SafetyStock = safetyStock,
                StockHealthStatus = healthStatus,
                AverageDailySales = avgDailySales,
                AverageMonthlySales = avgMonthlySales,
                Recent30DaysSales = salesLast30d,
                Previous30DaysSales = salesPrev30d,
                SalesTrendPercentage = salesTrendPct,
                PendingSalesOrdersQty = pendingSalesQty,
                SupplierLeadTimeDays = leadTimeDays,
                ExpectedDemandDuringLeadTime = expectedDemandDuringLeadTime,
                IncomingStock = incomingStock,
                RecommendedPurchaseQuantity = recommendedPurchaseQty,
                EstimatedDaysUntilStockout = estimatedDaysUntilStockout,
                PreferredSupplierId = preferredSupplierId,
                PreferredSupplierName = preferredSupplierName,
                PreferredSupplierCode = preferredSupplierCode,
                LastPurchasePrice = lastPurchasePrice,
                AverageHistoricalPrice = avgHistoricalPrice,
                CurrentSupplierPrice = currentSupplierPrice,
                PriceChangePercentage = priceChangePct,
                AiRecommendation = aiRecommendation,
                ReasonForRecommendation = reason,
                DataQuality = dataQuality,
                HasSufficientData = hasSufficientData,
                WarehouseStocks = whBreakdown,
                ExplanationDetails = explanation
            });
        }

        // Sort by urgency: Critical first, then Low with recommended qty > 0, then by estimated days to stockout
        return recommendations
            .OrderBy(r => r.StockHealthStatus == "Critical" ? 0 : (r.StockHealthStatus == "Low" ? 1 : 2))
            .ThenByDescending(r => r.RecommendedPurchaseQuantity)
            .ThenBy(r => r.EstimatedDaysUntilStockout)
            .ToList();
    }

    public async Task<List<DemandTrendDto>> GetDemandTrendsAsync(CancellationToken cancellationToken = default)
    {
        var businessId = RequireBusinessId();
        var now = _dateTimeProvider.UtcDateTime;
        var date7d = now.AddDays(-7);
        var date30d = now.AddDays(-30);
        var date60d = now.AddDays(-60);
        var date90d = now.AddDays(-90);
        var date180d = now.AddDays(-180);
        var date365d = now.AddDays(-365);

        var products = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.UnitOfMeasure)
            .Where(p => p.BusinessId == businessId && p.IsActive)
            .ToListAsync(cancellationToken);

        var invoiceItems = await _context.SalesInvoiceItems
            .AsNoTracking()
            .Include(i => i.SalesInvoice)
            .Where(i => i.SalesInvoice != null &&
                        i.SalesInvoice.BusinessId == businessId &&
                        i.SalesInvoice.Status != InvoiceStatus.Cancelled &&
                        i.SalesInvoice.InvoiceDate >= date365d)
            .ToListAsync(cancellationToken);

        var result = new List<DemandTrendDto>();

        foreach (var p in products)
        {
            var pItems = invoiceItems.Where(i => i.ProductId == p.Id).ToList();

            var q7d = pItems.Where(i => i.SalesInvoice!.InvoiceDate >= date7d).Sum(i => i.Quantity);
            var q30d = pItems.Where(i => i.SalesInvoice!.InvoiceDate >= date30d).Sum(i => i.Quantity);
            var qPrev30d = pItems.Where(i => i.SalesInvoice!.InvoiceDate >= date60d && i.SalesInvoice.InvoiceDate < date30d).Sum(i => i.Quantity);
            var q90d = pItems.Where(i => i.SalesInvoice!.InvoiceDate >= date90d).Sum(i => i.Quantity);
            var q180d = pItems.Where(i => i.SalesInvoice!.InvoiceDate >= date180d).Sum(i => i.Quantity);
            var q365d = pItems.Sum(i => i.Quantity);

            decimal growthRate30d = 0m;
            if (qPrev30d > 0)
            {
                growthRate30d = Math.Round(((q30d - qPrev30d) / qPrev30d) * 100m, 1);
            }
            else if (q30d > 0)
            {
                growthRate30d = 100m;
            }

            string trendDirection;
            bool isSpike = false;
            bool isDrop = false;

            if (growthRate30d >= 50m && q30d > 10m)
            {
                trendDirection = "Spike";
                isSpike = true;
            }
            else if (growthRate30d <= -50m && qPrev30d > 10m)
            {
                trendDirection = "Drop";
                isDrop = true;
            }
            else if (growthRate30d >= 10m)
            {
                trendDirection = "Increasing";
            }
            else if (growthRate30d <= -10m)
            {
                trendDirection = "Decreasing";
            }
            else
            {
                trendDirection = "Stable";
            }

            var dailyVelocity = Math.Round(q30d / 30m, 2);

            string uom = !string.IsNullOrWhiteSpace(p.UnitOfMeasure?.Code)
                ? p.UnitOfMeasure.Code
                : (!string.IsNullOrWhiteSpace(p.UnitOfMeasure?.Name) ? p.UnitOfMeasure.Name : "Units");

            string description;
            if (q365d == 0)
            {
                description = "No sales history recorded in the past 12 months.";
            }
            else if (growthRate30d > 0)
            {
                description = $"Sales of {p.Name} increased by approximately {Math.Abs(growthRate30d):0.#}% during the last 30 days compared with the previous 30-day period ({q30d} {uom} vs {qPrev30d} {uom}).";
            }
            else if (growthRate30d < 0)
            {
                description = $"Sales of {p.Name} decreased by approximately {Math.Abs(growthRate30d):0.#}% during the last 30 days compared with the previous 30-day period ({q30d} {uom} vs {qPrev30d} {uom}).";
            }
            else
            {
                description = $"Demand for {p.Name} remained steady ({q30d} {uom} consumed in the last 30 days).";
            }

            var dataQuality = q90d > 0 ? "High" : (q365d > 0 ? "Limited" : "Insufficient");

            result.Add(new DemandTrendDto
            {
                ProductId = p.Id,
                ProductName = p.Name,
                SKU = p.SKU,
                CategoryName = p.Category?.Name ?? "General",
                UnitOfMeasure = uom,
                Last7DaysQty = q7d,
                Last30DaysQty = q30d,
                Previous30DaysQty = qPrev30d,
                Last90DaysQty = q90d,
                Last6MonthsQty = q180d,
                PreviousYearQty = q365d,
                GrowthRate30DaysPercentage = growthRate30d,
                TrendDirection = trendDirection,
                TrendDescription = description,
                SalesVelocityPerDay = dailyVelocity,
                IsSpike = isSpike,
                IsDrop = isDrop,
                DataQuality = dataQuality
            });
        }

        return result
            .OrderByDescending(r => Math.Abs(r.GrowthRate30DaysPercentage))
            .ToList();
    }

    public async Task<List<SupplierPerformanceDto>> GetSupplierPerformancesAsync(CancellationToken cancellationToken = default)
    {
        var businessId = RequireBusinessId();

        var suppliers = await _context.Suppliers
            .AsNoTracking()
            .Where(s => s.BusinessId == businessId && s.IsActive)
            .ToListAsync(cancellationToken);

        var purchaseOrders = await _context.PurchaseOrders
            .AsNoTracking()
            .Include(po => po.Items)
            .Where(po => po.BusinessId == businessId && po.Status != PurchaseOrderStatus.Cancelled)
            .ToListAsync(cancellationToken);

        var grns = await _context.GoodsReceiptNotes
            .AsNoTracking()
            .Include(g => g.Items)
            .Include(g => g.PurchaseOrder)
            .Where(g => g.BusinessId == businessId)
            .ToListAsync(cancellationToken);

        var result = new List<SupplierPerformanceDto>();

        foreach (var supplier in suppliers)
        {
            var sOrders = purchaseOrders.Where(po => po.SupplierId == supplier.Id).ToList();
            var sGRNs = grns.Where(g => g.SupplierId == supplier.Id).ToList();

            var totalOrders = sOrders.Count;
            var totalSpend = sOrders.Sum(po => po.TotalAmount);
            var totalVolume = sOrders.SelectMany(po => po.Items).Sum(i => i.OrderedQuantity);
            var avgOrderValue = totalOrders > 0 ? Math.Round(totalSpend / totalOrders, 2) : 0m;

            // Delivery timeliness
            var deliveryDaysList = new List<double>();
            int onTimeCount = 0;
            int evaluatedGRNCount = 0;

            foreach (var grn in sGRNs)
            {
                if (grn.PurchaseOrder != null)
                {
                    evaluatedGRNCount++;
                    var daysToDeliver = (grn.ReceiptDate - grn.PurchaseOrder.OrderDate).TotalDays;
                    if (daysToDeliver >= 0)
                    {
                        deliveryDaysList.Add(daysToDeliver);
                    }

                    if (grn.PurchaseOrder.ExpectedDeliveryDate.HasValue)
                    {
                        // On-time if received on or before expected delivery date + 1 grace day
                        if (grn.ReceiptDate <= grn.PurchaseOrder.ExpectedDeliveryDate.Value.AddDays(1))
                        {
                            onTimeCount++;
                        }
                    }
                    else
                    {
                        // Default threshold 10 days
                        if (daysToDeliver <= 10)
                        {
                            onTimeCount++;
                        }
                    }
                }
            }

            var avgDeliveryDays = deliveryDaysList.Any() ? Math.Round(deliveryDaysList.Average(), 1) : 7.0;
            var onTimeRate = evaluatedGRNCount > 0
                ? Math.Round(((decimal)onTimeCount / evaluatedGRNCount) * 100m, 1)
                : 100m;

            // Quality & Rejections
            var allGRNItems = sGRNs.SelectMany(g => g.Items).ToList();
            var totalReceived = allGRNItems.Sum(i => i.ReceivedQuantity);
            var totalRejected = allGRNItems.Sum(i => i.RejectedQuantity);
            var rejectionRate = totalReceived > 0
                ? Math.Round((totalRejected / totalReceived) * 100m, 1)
                : 0m;

            var qualityNotes = allGRNItems
                .Where(i => !string.IsNullOrWhiteSpace(i.RejectionReason))
                .Select(i => i.RejectionReason!)
                .Distinct()
                .Take(5)
                .ToList();

            // Average Unit Price & Price Consistency
            var orderItems = sOrders.SelectMany(po => po.Items).ToList();
            var avgPrice = orderItems.Any()
                ? Math.Round(orderItems.Average(i => i.UnitPrice), 2)
                : 0m;

            string priceConsistency = "Stable";
            if (orderItems.Count > 1)
            {
                var minPrice = orderItems.Min(i => i.UnitPrice);
                var maxPrice = orderItems.Max(i => i.UnitPrice);
                var variance = minPrice > 0 ? ((maxPrice - minPrice) / minPrice) * 100m : 0m;
                if (variance > 25m) priceConsistency = "Volatile";
                else if (variance > 10m) priceConsistency = "Moderate Variance";
            }

            var hasLongDelivery = avgDeliveryDays > 10 || onTimeRate < 80m;

            // Summary text
            string summary;
            if (totalOrders == 0)
            {
                summary = "New supplier profile. No historical procurement data recorded yet.";
            }
            else
            {
                summary = $"{supplier.Name} has completed {totalOrders} orders (₹{totalSpend:N0} total spend). Average delivery cycle is {avgDeliveryDays:0.#} days with {onTimeRate:0.#}% on-time compliance. Quality rejection rate is {rejectionRate:0.#}%.";
            }

            result.Add(new SupplierPerformanceDto
            {
                SupplierId = supplier.Id,
                SupplierName = supplier.Name,
                SupplierCode = supplier.SupplierCode,
                ContactPerson = supplier.ContactPerson,
                Email = supplier.Email,
                Phone = supplier.Phone,
                PaymentTermsDays = supplier.PaymentTermsDays,
                OutstandingPayable = supplier.OutstandingPayable,
                TotalPurchaseOrdersCount = totalOrders,
                TotalPurchaseVolumeQty = totalVolume,
                TotalSpendAmount = totalSpend,
                AverageOrderValue = avgOrderValue,
                AverageDeliveryDays = avgDeliveryDays,
                AgreedLeadTimeDays = 7,
                OnTimeDeliveryPercentage = onTimeRate,
                HasLongDeliveryTimes = hasLongDelivery,
                TotalReceivedQty = totalReceived,
                TotalRejectedQty = totalRejected,
                RejectionPercentage = rejectionRate,
                QualityIssuesCount = qualityNotes.Count,
                RecentQualityNotes = qualityNotes,
                AveragePurchasePrice = avgPrice,
                PriceConsistency = priceConsistency,
                PerformanceSummary = summary,
                DataQuality = totalOrders >= 3 ? "High" : (totalOrders > 0 ? "Limited" : "Insufficient")
            });
        }

        return result
            .OrderByDescending(s => s.TotalSpendAmount)
            .ToList();
    }

    public async Task<QuotationAnalysisResultDto> AnalyzeQuotationAsync(QuotationAnalysisRequestDto request, CancellationToken cancellationToken = default)
    {
        var businessId = RequireBusinessId();

        // 1. Find Product
        Product? product = null;
        if (request.ProductId.HasValue)
        {
            product = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.UnitOfMeasure)
                .FirstOrDefaultAsync(p => p.Id == request.ProductId.Value && p.BusinessId == businessId, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(request.ProductName))
        {
            product = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.UnitOfMeasure)
                .FirstOrDefaultAsync(p => p.BusinessId == businessId &&
                                         (p.Name.ToLower().Contains(request.ProductName.ToLower()) ||
                                          p.SKU.ToLower().Contains(request.ProductName.ToLower())), cancellationToken);
        }

        // Financial calculations of Quotation
        var grossUnitPrice = request.QuotedUnitPrice;
        var discountAmt = grossUnitPrice * (request.DiscountPercentage / 100m);
        var netUnitPrice = grossUnitPrice - discountAmt;
        var taxAmt = netUnitPrice * (request.TaxPercentage / 100m);
        var effectiveUnitPriceWithTax = netUnitPrice + taxAmt;
        var totalEstimatedCost = Math.Round(netUnitPrice * request.QuotedQuantity * (1m + request.TaxPercentage / 100m), 2);

        // Fetch Historical PO Items for this product
        var historicalItems = new List<PurchaseOrderItem>();
        if (product != null)
        {
            historicalItems = await _context.PurchaseOrderItems
                .AsNoTracking()
                .Include(poi => poi.PurchaseOrder)
                    .ThenInclude(po => po!.Supplier)
                .Where(poi => poi.ProductId == product.Id &&
                              poi.PurchaseOrder != null &&
                              poi.PurchaseOrder.BusinessId == businessId &&
                              poi.PurchaseOrder.Status != PurchaseOrderStatus.Cancelled)
                .OrderByDescending(poi => poi.PurchaseOrder!.OrderDate)
                .ToListAsync(cancellationToken);
        }

        decimal histAvgPrice = product?.PurchasePrice ?? 0m;
        decimal lastPrice = product?.PurchasePrice ?? 0m;
        decimal lowestPrice = product?.PurchasePrice ?? 0m;

        if (historicalItems.Any())
        {
            histAvgPrice = Math.Round(historicalItems.Average(i => i.UnitPrice), 2);
            lastPrice = historicalItems.First().UnitPrice;
            lowestPrice = historicalItems.Min(i => i.UnitPrice);
        }

        decimal priceVariancePct = 0m;
        if (histAvgPrice > 0)
        {
            priceVariancePct = Math.Round(((netUnitPrice - histAvgPrice) / histAvgPrice) * 100m, 1);
        }

        // Assessments & Flags
        var flags = new List<string>();
        string priceAssessment;

        if (priceVariancePct > 15m)
        {
            priceAssessment = "Significantly Higher than Baseline";
            flags.Add("Price Spike Alert");
        }
        else if (priceVariancePct > 3m)
        {
            priceAssessment = "Slightly Higher than Baseline";
            flags.Add("Price Increase");
        }
        else if (priceVariancePct < -10m)
        {
            priceAssessment = "Favorable Discount";
            flags.Add("Price Advantage");
        }
        else if (priceVariancePct < -2m)
        {
            priceAssessment = "Below Historical Average";
            flags.Add("Price Decrease");
        }
        else
        {
            priceAssessment = "Competitive Market Standard";
            flags.Add("Standard Market Price");
        }

        // Lead time assessment
        string leadTimeAssessment;
        if (request.QuotedLeadTimeDays > 14)
        {
            leadTimeAssessment = $"Extended lead time ({request.QuotedLeadTimeDays} days). May increase stockout exposure.";
            flags.Add("Delivery-Time Warning");
        }
        else if (request.QuotedLeadTimeDays <= 5)
        {
            leadTimeAssessment = $"Fast delivery timeline ({request.QuotedLeadTimeDays} days). Supports prompt replenishment.";
            flags.Add("Fast Delivery");
        }
        else
        {
            leadTimeAssessment = $"Standard industry lead time ({request.QuotedLeadTimeDays} days).";
        }

        // Payment terms
        string paymentTermsAssessment = request.PaymentTerms.Contains("Advance", StringComparison.OrdinalIgnoreCase)
            ? "Requires upfront capital commitment."
            : "Standard trade credit terms.";

        if (request.PaymentTerms.Contains("Advance", StringComparison.OrdinalIgnoreCase))
        {
            flags.Add("Payment-Term Difference");
        }

        // AI Explanation
        string aiExplanation;
        var pName = product?.Name ?? request.ProductName;

        if (histAvgPrice == 0)
        {
            aiExplanation = $"No prior purchase history exists for {pName}. Quoted rate of ₹{netUnitPrice:N2} cannot be verified against historical records. Validate against market benchmarks before proceeding.";
        }
        else if (priceVariancePct > 0)
        {
            aiExplanation = $"The quoted net unit price (₹{netUnitPrice:N2}) is approximately {Math.Abs(priceVariancePct):0.1}% higher than the historical average purchase price (₹{histAvgPrice:N2}) for {pName}. Last recorded purchase was at ₹{lastPrice:N2}.";
        }
        else if (priceVariancePct < 0)
        {
            aiExplanation = $"The quoted net unit price (₹{netUnitPrice:N2}) is approximately {Math.Abs(priceVariancePct):0.1}% below the historical average purchase price (₹{histAvgPrice:N2}) for {pName}. Historical lowest was ₹{lowestPrice:N2}.";
        }
        else
        {
            aiExplanation = $"The quoted net unit price (₹{netUnitPrice:N2}) aligns closely with your historical average purchase price (₹{histAvgPrice:N2}) for {pName}.";
        }

        // Alternative Suppliers Comparison
        var altSuppliers = historicalItems
            .GroupBy(i => i.PurchaseOrder!.Supplier)
            .Where(g => g.Key != null)
            .Select(g => new QuotationSupplierComparisonDto
            {
                SupplierId = g.Key!.Id,
                SupplierName = g.Key.Name,
                LastPriceGiven = g.OrderByDescending(x => x.PurchaseOrder!.OrderDate).First().UnitPrice,
                AveragePriceGiven = Math.Round(g.Average(x => x.UnitPrice), 2),
                AverageLeadTimeDays = 7.0,
                OnTimeDeliveryRate = 95m,
                LastOrderDate = g.OrderByDescending(x => x.PurchaseOrder!.OrderDate).First().PurchaseOrder!.OrderDate
            })
            .Take(4)
            .ToList();

        var overallAssessment = flags.Contains("Price Spike Alert")
            ? "Consider negotiating or requesting counter-quotes from historical vendors."
            : (flags.Contains("Price Advantage")
                ? "Favorable pricing confirmed against ERP historical data."
                : "Quotation is within acceptable standard ERP variance.");

        return new QuotationAnalysisResultDto
        {
            ProductId = product?.Id,
            ProductName = pName,
            SupplierName = request.SupplierName,
            QuotedGrossPrice = grossUnitPrice,
            DiscountAmount = discountAmt,
            QuotedNetUnitPrice = netUnitPrice,
            TaxAmount = taxAmt,
            TotalEstimatedCost = totalEstimatedCost,
            HistoricalAveragePrice = histAvgPrice,
            LastPurchasePrice = lastPrice,
            LowestHistoricalPrice = lowestPrice,
            PriceVariancePercentage = priceVariancePct,
            PriceAssessment = priceAssessment,
            LeadTimeAssessment = leadTimeAssessment,
            PaymentTermsAssessment = paymentTermsAssessment,
            OverallAssessment = overallAssessment,
            AiExplanation = aiExplanation,
            KeyFlags = flags,
            AlternativeSuppliers = altSuppliers,
            DataQuality = historicalItems.Any() ? "High" : "Insufficient"
        };
    }

    public async Task<AiChatResponseDto> AskAssistantAsync(AiChatRequestDto request, CancellationToken cancellationToken = default)
    {
        var q = (request.Question ?? string.Empty).Trim().ToLower();
        var reorders = await GetSmartReordersAsync(cancellationToken);
        var trends = await GetDemandTrendsAsync(cancellationToken);
        var suppliers = await GetSupplierPerformancesAsync(cancellationToken);

        var referencedProducts = new List<SmartReorderRecommendationDto>();
        string answer;
        var followups = new List<string>();

        if (q.Contains("running low") || q.Contains("low stock") || q.Contains("critical"))
        {
            var lowItems = reorders.Where(r => r.StockHealthStatus == "Critical" || r.StockHealthStatus == "Low").ToList();
            referencedProducts = lowItems.Take(6).ToList();

            if (!lowItems.Any())
            {
                answer = "### Stock Health Assessment\n\nAll inventory levels are currently in a **Healthy** state! There are no products below their reorder threshold or facing imminent stockout.";
            }
            else
            {
                answer = $"### ⚠️ Inventory Shortage Warning\n\nFound **{lowItems.Count} product(s)** running below reorder thresholds:\n\n";
                foreach (var item in lowItems.Take(5))
                {
                    answer += $"* **{item.ProductName}** ({item.SKU}): Available stock is **{item.AvailableStock} {item.UnitOfMeasure}** (Reorder level: {item.ReorderLevel}). Estimated stockout in **{item.EstimatedDaysUntilStockout:0} days**. Recommended purchase: **{item.RecommendedPurchaseQuantity} {item.UnitOfMeasure}** from *{item.PreferredSupplierName}*.\n";
                }

                if (lowItems.Count > 5)
                {
                    answer += $"\n*...and {lowItems.Count - 5} more items requiring replenishment attention.*";
                }
            }

            followups.Add("What should I purchase this week?");
            followups.Add("Show products that may become out of stock within 10 days.");
            followups.Add("Which suppliers have the fastest delivery lead time?");
        }
        else if (q.Contains("purchase this week") || q.Contains("what should i purchase") || q.Contains("recommended purchase"))
        {
            var purchaseItems = reorders.Where(r => r.RecommendedPurchaseQuantity > 0).ToList();
            referencedProducts = purchaseItems.Take(6).ToList();

            if (!purchaseItems.Any())
            {
                answer = "### Weekly Procurement Plan\n\nBased on sales velocity, safety buffers, and existing open purchase orders, **no immediate purchases are required this week**. Current stock levels adequately cover expected lead-time demand.";
            }
            else
            {
                var totalCost = purchaseItems.Sum(r => r.RecommendedPurchaseQuantity * (r.CurrentSupplierPrice > 0 ? r.CurrentSupplierPrice : r.LastPurchasePrice));
                answer = $"### 💡 Weekly Purchase Recommendation\n\nRecommended reorders for **{purchaseItems.Count} item(s)** totaling approximately **₹{totalCost:N2}**:\n\n";

                foreach (var item in purchaseItems.Take(5))
                {
                    var price = item.CurrentSupplierPrice > 0 ? item.CurrentSupplierPrice : item.LastPurchasePrice;
                    answer += $"* **{item.ProductName}**: Buy **{item.RecommendedPurchaseQuantity} {item.UnitOfMeasure}** @ ~₹{price:N2} (Est: ₹{(item.RecommendedPurchaseQuantity * price):N0}). Preferred Vendor: **{item.PreferredSupplierName}** (Lead time: {item.SupplierLeadTimeDays}d).\n  > *Reason:* {item.ReasonForRecommendation}\n\n";
                }

                answer += "\n*Note: AI recommendations are advisory. Click 'Create Purchase Request' to verify and generate an official PO.*";
            }

            followups.Add("Why is the first product recommended?");
            followups.Add("Which suppliers gave better prices historically?");
            followups.Add("Show products with increasing demand.");
        }
        else if (q.Contains("increasing demand") || q.Contains("demand spike") || q.Contains("sales growth"))
        {
            var incTrends = trends.Where(t => t.TrendDirection == "Increasing" || t.IsSpike).OrderByDescending(t => t.GrowthRate30DaysPercentage).ToList();

            if (!incTrends.Any())
            {
                answer = "### Demand Analysis\n\nSales demand across all active product categories is currently **stable**. No sudden surges or sharp upward shifts exceeding 10% were detected over the past 30-day period.";
            }
            else
            {
                answer = $"### 📈 Products with Rising Demand\n\nIdentified **{incTrends.Count} product(s)** with significant upward sales momentum:\n\n";
                foreach (var t in incTrends.Take(5))
                {
                    answer += $"* **{t.ProductName}** ({t.SKU}): **+{t.GrowthRate30DaysPercentage:0.1}%** growth in the last 30 days ({t.Last30DaysQty} {t.UnitOfMeasure} vs {t.Previous30DaysQty} {t.UnitOfMeasure} previous period). Current velocity: ~{t.SalesVelocityPerDay:0.#} {t.UnitOfMeasure}/day.\n";
                }
            }

            followups.Add("Which of these products need to be reordered?");
            followups.Add("Show products whose purchase price increased recently.");
        }
        else if (q.Contains("price increased") || q.Contains("cost increased") || q.Contains("higher price"))
        {
            var costIncreasing = reorders.Where(r => r.PriceChangePercentage > 0).OrderByDescending(r => r.PriceChangePercentage).ToList();

            if (!costIncreasing.Any())
            {
                answer = "### Purchase Price Analysis\n\nPurchase costs across your catalog have remained **consistent and stable**. No recent vendor price hikes were detected against your baseline averages.";
            }
            else
            {
                answer = $"### 💰 Products with Increasing Purchase Costs\n\nFound **{costIncreasing.Count} item(s)** where current vendor pricing exceeds historical baselines:\n\n";
                foreach (var item in costIncreasing.Take(5))
                {
                    answer += $"* **{item.ProductName}**: Current cost **₹{item.CurrentSupplierPrice:N2}** vs Historical avg **₹{item.AverageHistoricalPrice:N2}** (**+{item.PriceChangePercentage:0.1}%**). Supplier: *{item.PreferredSupplierName}*.\n";
                }
            }

            followups.Add("Which suppliers gave better prices historically?");
            followups.Add("Compare supplier quotations.");
        }
        else if (q.Contains("supplier") && (q.Contains("better") || q.Contains("price") || q.Contains("historically") || q.Contains("performance")))
        {
            answer = "### 🚚 Supplier Performance & Price Reliability\n\nAnalysis of vendor fulfillment records and billing histories:\n\n";
            foreach (var s in suppliers.Take(5))
            {
                answer += $"* **{s.SupplierName}** ({s.SupplierCode}):\n  - On-Time Delivery Rate: **{s.OnTimeDeliveryPercentage:0.1}%**\n  - Average Lead Time: **{s.AverageDeliveryDays:0.#} days**\n  - Quality Rejection: **{s.RejectionPercentage:0.1}%**\n  - Price Stability: **{s.PriceConsistency}** (Avg Order: ₹{s.AverageOrderValue:N0})\n\n";
            }

            followups.Add("Which products are running low?");
            followups.Add("What should I purchase this week?");
        }
        else if (q.Contains("within 10 days") || q.Contains("10 days") || q.Contains("stockout"))
        {
            var urgent = reorders.Where(r => r.EstimatedDaysUntilStockout <= 10m && r.AvailableStock > 0).ToList();
            referencedProducts = urgent;

            if (!urgent.Any())
            {
                answer = "### Stockout Horizon (Next 10 Days)\n\nGreat news! **Zero products** are projected to run out of stock within the next 10 days based on active consumption velocity.";
            }
            else
            {
                answer = $"### ⏱ Products at Risk of Stockout within 10 Days\n\nFound **{urgent.Count} item(s)** approaching depletion:\n\n";
                foreach (var item in urgent)
                {
                    answer += $"* **{item.ProductName}**: **{item.AvailableStock} {item.UnitOfMeasure}** remaining (~**{item.EstimatedDaysUntilStockout:0} days** of stock). Lead time is **{item.SupplierLeadTimeDays} days**.\n";
                }
            }

            followups.Add("What should I purchase this week?");
            followups.Add("Show products with increasing demand.");
        }
        else if (q.Contains("why") && request.ProductId.HasValue)
        {
            var item = reorders.FirstOrDefault(r => r.ProductId == request.ProductId.Value);
            if (item != null)
            {
                referencedProducts = new List<SmartReorderRecommendationDto> { item };
                answer = $"### Explainable AI: Recommendation for {item.ProductName}\n\n" +
                         $"* **Recommendation**: {item.AiRecommendation}\n" +
                         $"* **Reason**: {item.ReasonForRecommendation}\n\n" +
                         $"#### Calculation Breakdown:\n" +
                         $"* **Formula**: Expected Demand During Lead Time + Safety Stock - Available Stock - Incoming Stock\n" +
                         $"* **Expected Demand**: {item.AverageDailySales:0.##} {item.UnitOfMeasure}/day × {item.SupplierLeadTimeDays} lead days = **{item.ExpectedDemandDuringLeadTime} {item.UnitOfMeasure}**\n" +
                         $"* **Safety Stock**: **{item.SafetyStock} {item.UnitOfMeasure}** (7 days buffer)\n" +
                         $"* **Available Stock**: **{item.AvailableStock} {item.UnitOfMeasure}** (Physical: {item.CurrentStock}, Reserved: {item.ReservedStock})\n" +
                         $"* **Incoming POs**: **{item.IncomingStock} {item.UnitOfMeasure}**\n" +
                         $"* **Calculated Purchase Quantity**: **{item.RecommendedPurchaseQuantity} {item.UnitOfMeasure}**\n" +
                         $"* **Data Quality**: **{item.DataQuality}**";
            }
            else
            {
                answer = "Could not locate the requested product in the current catalog.";
            }

            followups.Add("What should I purchase this week?");
            followups.Add("Compare supplier quotations.");
        }
        else
        {
            // Default intelligent conversational summary
            var criticalCount = reorders.Count(r => r.StockHealthStatus == "Critical");
            var lowCount = reorders.Count(r => r.StockHealthStatus == "Low");
            var recCount = reorders.Count(r => r.RecommendedPurchaseQuantity > 0);

            answer = $"### BizFlow AI Smart Purchase Assistant\n\n" +
                     $"I have analyzed your business's inventory, sales velocity, supplier delivery metrics, and pending purchase orders.\n\n" +
                     $"* 🔴 **Critical Stock**: {criticalCount} item(s)\n" +
                     $"* 🟠 **Low Stock**: {lowCount} item(s)\n" +
                     $"* 💡 **Recommended Purchases**: {recCount} item(s)\n\n" +
                     $"How can I assist you with your procurement decisions today? You can ask me:\n" +
                     $"- *'Which products are running low?'*\n" +
                     $"- *'What should I purchase this week?'*\n" +
                     $"- *'Which products have increasing demand?'*\n" +
                     $"- *'Show products whose purchase price increased recently.'*\n" +
                     $"- *'Which suppliers gave better prices historically?'*";

            followups.Add("Which products are running low?");
            followups.Add("What should I purchase this week?");
            followups.Add("Which suppliers gave better prices historically?");
        }

        return new AiChatResponseDto
        {
            Answer = answer,
            SuggestedFollowups = followups,
            ReferencedProducts = referencedProducts,
            DataQuality = "High",
            Timestamp = _dateTimeProvider.UtcDateTime
        };
    }
}
