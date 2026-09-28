using System;
using System.Collections.Generic;

namespace BizFlow.Application.DTOs.AiAssistant;

public class SmartPurchaseDashboardDto
{
    public InsightSummaryKpisDto SummaryKpis { get; set; } = new();
    public List<SmartReorderRecommendationDto> ReorderRecommendations { get; set; } = new();
    public List<DemandTrendDto> DemandTrends { get; set; } = new();
    public List<SupplierPerformanceDto> SupplierPerformances { get; set; } = new();
    public List<CostTrendProductDto> CostTrendProducts { get; set; } = new();
    public decimal EstimatedTotalReorderCost { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class InsightSummaryKpisDto
{
    public int CriticalStockCount { get; set; }
    public int LowStockCount { get; set; }
    public int HealthyStockCount { get; set; }
    public int OverstockedCount { get; set; }
    public int IncreasingDemandCount { get; set; }
    public int DecreasingDemandCount { get; set; }
    public int CostIncreasingCount { get; set; }
    public int DelayedSuppliersCount { get; set; }
    public int RecommendedPurchasesCount { get; set; }
}

public class SmartReorderRecommendationDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;

    // Stock metrics
    public decimal CurrentStock { get; set; }
    public decimal ReservedStock { get; set; }
    public decimal AvailableStock { get; set; }
    public decimal ReorderLevel { get; set; }
    public decimal MaxStockLevel { get; set; }
    public decimal SafetyStock { get; set; }
    public string StockHealthStatus { get; set; } = "Healthy"; // Critical, Low, Healthy, Overstocked

    // Demand & Consumption
    public decimal AverageDailySales { get; set; }
    public decimal AverageMonthlySales { get; set; }
    public decimal Recent30DaysSales { get; set; }
    public decimal Previous30DaysSales { get; set; }
    public decimal SalesTrendPercentage { get; set; }
    public decimal PendingSalesOrdersQty { get; set; }

    // Supplier & Procurement
    public int SupplierLeadTimeDays { get; set; }
    public decimal ExpectedDemandDuringLeadTime { get; set; }
    public decimal IncomingStock { get; set; } // Pending POs
    public decimal RecommendedPurchaseQuantity { get; set; }
    public decimal EstimatedDaysUntilStockout { get; set; }

    // Preferred Vendor & Pricing
    public Guid? PreferredSupplierId { get; set; }
    public string PreferredSupplierName { get; set; } = "Not Assigned";
    public string PreferredSupplierCode { get; set; } = string.Empty;
    public decimal LastPurchasePrice { get; set; }
    public decimal AverageHistoricalPrice { get; set; }
    public decimal CurrentSupplierPrice { get; set; }
    public decimal PriceChangePercentage { get; set; }

    // Recommendation & Explainability
    public string AiRecommendation { get; set; } = string.Empty; // e.g., "Consider purchasing", "Reorder urgently", "Hold"
    public string ReasonForRecommendation { get; set; } = string.Empty;
    public string DataQuality { get; set; } = "High"; // High, Limited, Insufficient
    public bool HasSufficientData { get; set; } = true;

    // Warehouse-wise Stock
    public List<WarehouseStockBreakdownDto> WarehouseStocks { get; set; } = new();

    // Deep Explainable AI Inspector
    public RecommendationExplanationDto ExplanationDetails { get; set; } = new();
}

public class WarehouseStockBreakdownDto
{
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityReserved { get; set; }
    public decimal QuantityAvailable { get; set; }
}

public class RecommendationExplanationDto
{
    public string FormulaUsed { get; set; } = string.Empty;
    public string ExpectedDemandFormula { get; set; } = string.Empty;
    public string SafetyStockFormula { get; set; } = string.Empty;
    public decimal AvailableStockValue { get; set; }
    public decimal IncomingStockValue { get; set; }
    public decimal NetShortage { get; set; }
    public string HistoricalPeriodEvaluated { get; set; } = "Last 90 Days sales velocity & last 12 months purchase history";
    public List<string> Assumptions { get; set; } = new();
    public List<string> DataPointsConsidered { get; set; } = new();
    public string RecommendedAction { get; set; } = string.Empty;
    public string DataQualityNote { get; set; } = string.Empty;
}

public class DemandTrendDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;

    public decimal Last7DaysQty { get; set; }
    public decimal Last30DaysQty { get; set; }
    public decimal Previous30DaysQty { get; set; }
    public decimal Last90DaysQty { get; set; }
    public decimal Last6MonthsQty { get; set; }
    public decimal PreviousYearQty { get; set; }

    public decimal GrowthRate30DaysPercentage { get; set; }
    public string TrendDirection { get; set; } = "Stable"; // Increasing, Decreasing, Stable, Spike, Drop
    public string TrendDescription { get; set; } = string.Empty;
    public decimal SalesVelocityPerDay { get; set; }
    public bool IsSpike { get; set; }
    public bool IsDrop { get; set; }
    public string DataQuality { get; set; } = "High";
}

public class SupplierPerformanceDto
{
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string SupplierCode { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public int PaymentTermsDays { get; set; }
    public decimal OutstandingPayable { get; set; }

    public int TotalPurchaseOrdersCount { get; set; }
    public decimal TotalPurchaseVolumeQty { get; set; }
    public decimal TotalSpendAmount { get; set; }
    public decimal AverageOrderValue { get; set; }

    public double AverageDeliveryDays { get; set; }
    public int AgreedLeadTimeDays { get; set; }
    public decimal OnTimeDeliveryPercentage { get; set; }
    public bool HasLongDeliveryTimes { get; set; }

    public decimal TotalReceivedQty { get; set; }
    public decimal TotalRejectedQty { get; set; }
    public decimal RejectionPercentage { get; set; }
    public int QualityIssuesCount { get; set; }
    public List<string> RecentQualityNotes { get; set; } = new();

    public decimal AveragePurchasePrice { get; set; }
    public string PriceConsistency { get; set; } = "Stable"; // Highly Stable, Moderate Variance, Volatile
    public string PerformanceSummary { get; set; } = string.Empty;
    public string DataQuality { get; set; } = "High";
}

public class CostTrendProductDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public decimal CurrentPurchasePrice { get; set; }
    public decimal PreviousPurchasePrice { get; set; }
    public decimal HistoricalAveragePrice { get; set; }
    public decimal PriceIncreasePercentage { get; set; }
    public DateTime? LastPurchaseDate { get; set; }
    public string PreferredSupplierName { get; set; } = string.Empty;
    public string CostAssessment { get; set; } = string.Empty;
}

public class QuotationAnalysisRequestDto
{
    public Guid? ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public Guid? SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public decimal QuotedQuantity { get; set; }
    public decimal QuotedUnitPrice { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal TaxPercentage { get; set; } = 18m;
    public string DeliveryLocation { get; set; } = string.Empty;
    public int QuotedLeadTimeDays { get; set; } = 7;
    public string PaymentTerms { get; set; } = "Net 30 Days";
    public int ValidityDays { get; set; } = 15;
    public string? Brand { get; set; }
    public string? ProductCategory { get; set; }
}

public class QuotationAnalysisResultDto
{
    public Guid? ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;

    public decimal QuotedGrossPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal QuotedNetUnitPrice { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalEstimatedCost { get; set; }

    public decimal HistoricalAveragePrice { get; set; }
    public decimal LastPurchasePrice { get; set; }
    public decimal LowestHistoricalPrice { get; set; }
    public decimal PriceVariancePercentage { get; set; }

    public string PriceAssessment { get; set; } = string.Empty; // Favorable, Higher than Average, Market Standard, Alert
    public string LeadTimeAssessment { get; set; } = string.Empty;
    public string PaymentTermsAssessment { get; set; } = string.Empty;
    public string OverallAssessment { get; set; } = string.Empty;
    public string AiExplanation { get; set; } = string.Empty;

    public List<string> KeyFlags { get; set; } = new(); // Price Increase, Price Decrease, Unusual Quotation, etc.
    public List<QuotationSupplierComparisonDto> AlternativeSuppliers { get; set; } = new();
    public string DataQuality { get; set; } = "High";
}

public class QuotationSupplierComparisonDto
{
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public decimal LastPriceGiven { get; set; }
    public decimal AveragePriceGiven { get; set; }
    public double AverageLeadTimeDays { get; set; }
    public decimal OnTimeDeliveryRate { get; set; }
    public DateTime? LastOrderDate { get; set; }
}

public class AiChatRequestDto
{
    public string Question { get; set; } = string.Empty;
    public Guid? ProductId { get; set; }
    public string? ModuleContext { get; set; }
}

public class AiChatResponseDto
{
    public string Answer { get; set; } = string.Empty;
    public List<string> SuggestedFollowups { get; set; } = new();
    public List<SmartReorderRecommendationDto> ReferencedProducts { get; set; } = new();
    public string DataQuality { get; set; } = "High";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
