using System;
using System.Collections.Generic;

namespace BizFlow.Application.DTOs.ClothHub;

// --- 1. DAILY / MONTHLY SALES REPORT DTOs ---

public class ClothDailySalesDayDto
{
    public DateTime Date { get; set; }
    public string DateFormatted { get; set; } = string.Empty;
    public int BillsCount { get; set; }
    public int GarmentsSoldCount { get; set; }
    public decimal GrossSales { get; set; }
    public decimal DiscountGiven { get; set; }
    public decimal TaxCollected { get; set; }
    public decimal ReturnsDeducted { get; set; }
    public decimal NetSales { get; set; }
    public decimal CashAmount { get; set; }
    public decimal DigitalAmount { get; set; }
    public decimal CreditAmount { get; set; }
}

public class ClothDailySalesReportDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal TotalNetSales { get; set; }
    public int TotalBillsCount { get; set; }
    public int TotalGarmentsSold { get; set; }
    public decimal TotalCashCollections { get; set; }
    public decimal TotalDigitalCollections { get; set; }
    public decimal TotalCreditSales { get; set; }
    public decimal AverageBasketValue { get; set; }
    public List<ClothDailySalesDayDto> DailyBreakdown { get; set; } = new();
}

// --- 2. PROFIT & MARGIN REPORT DTOs ---

public class ClothProfitMarginItemDto
{
    public Guid VariantId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string SizeName { get; set; } = string.Empty;
    public string ColourName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal SellingRevenue { get; set; }
    public decimal EstimatedCostOfGoodsSold { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal MarginPercentage { get; set; }
}

public class ClothBrandProfitDto
{
    public string BrandName { get; set; } = string.Empty;
    public int PiecesSold { get; set; }
    public decimal Revenue { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal MarginPercentage { get; set; }
}

public class ClothProfitMarginReportDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalCogs { get; set; }
    public decimal TotalGrossProfit { get; set; }
    public decimal OverallMarginPercentage { get; set; }
    public List<ClothBrandProfitDto> BrandPerformance { get; set; } = new();
    public List<ClothProfitMarginItemDto> TopProfitableItems { get; set; } = new();
}

// --- 3. SIZE & COLOUR VELOCITY / PERFORMANCE DTOs ---

public class ClothSizePerformanceDto
{
    public string SizeName { get; set; } = string.Empty;
    public int PiecesSold { get; set; }
    public decimal Revenue { get; set; }
    public decimal SharePercentage { get; set; }
    public string VelocityStatus { get; set; } = "Normal"; // "Fast Moving", "Normal", "Slow Moving"
}

public class ClothColourPerformanceDto
{
    public string ColourName { get; set; } = string.Empty;
    public string ColourHex { get; set; } = "#ffffff";
    public int PiecesSold { get; set; }
    public decimal Revenue { get; set; }
    public decimal SharePercentage { get; set; }
}

public class ClothSizeColourPerformanceReportDto
{
    public int TotalGarmentsSold { get; set; }
    public List<ClothSizePerformanceDto> SizeRankings { get; set; } = new();
    public List<ClothColourPerformanceDto> ColourRankings { get; set; } = new();
}

// --- 4. APPAREL GST REPORT DTOs ---

public class ClothGstSlabSummaryDto
{
    public decimal GstRate { get; set; } // 5% or 12%
    public decimal TaxableAmount { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal TotalTax { get; set; }
    public decimal TotalInvoiceValue { get; set; }
    public int InvoiceCount { get; set; }
}

public class ClothHsnSummaryDto
{
    public string HsnCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Uqc { get; set; } = "PCS";
    public int TotalQuantity { get; set; }
    public decimal TotalValue { get; set; }
    public decimal TaxableValue { get; set; }
    public decimal IntegratedTax { get; set; }
    public decimal CentralTax { get; set; }
    public decimal StateTax { get; set; }
}

public class ClothApparelGstReportDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal TotalTaxableValue { get; set; }
    public decimal TotalCgst { get; set; }
    public decimal TotalSgst { get; set; }
    public decimal TotalGstAmount { get; set; }
    public decimal TotalGrossWithGst { get; set; }
    public List<ClothGstSlabSummaryDto> Slabs { get; set; } = new();
    public List<ClothHsnSummaryDto> HsnSummaries { get; set; } = new();
}

// --- 5. CUSTOMER DTOs ---

public class ClothCustomerDto
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Gstin { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal CurrentOutstanding { get; set; }
    public decimal AvailableCredit => Math.Max(0, CreditLimit - CurrentOutstanding);
    public decimal TotalSpentAmount { get; set; }
    public int TotalVisitsCount { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public DateTime? AnniversaryDate { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
}

public class CreateClothCustomerDto
{
    public string CustomerName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Gstin { get; set; }
    public decimal CreditLimit { get; set; } = 5000;
    public DateTime? DateOfBirth { get; set; }
    public DateTime? AnniversaryDate { get; set; }
    public string? Notes { get; set; }
}

public class SettleCustomerCreditDto
{
    public Guid CustomerId { get; set; }
    public decimal PaymentAmount { get; set; }
    public string PaymentMode { get; set; } = "Cash"; // "Cash", "UPI", "Bank Transfer"
    public string? Notes { get; set; }
}

// --- 6. EXPENSE DTOs ---

public class ClothExpenseDto
{
    public Guid Id { get; set; }
    public string VoucherNumber { get; set; } = string.Empty;
    public DateTime ExpenseDate { get; set; }
    public string ExpenseCategory { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentMode { get; set; } = string.Empty;
    public string? PaidTo { get; set; }
    public string? Notes { get; set; }
    public string? ApprovedBy { get; set; }
}

public class CreateClothExpenseDto
{
    public DateTime? ExpenseDate { get; set; }
    public string ExpenseCategory { get; set; } = "Staff Welfare / Tea & Snacks";
    public decimal Amount { get; set; }
    public string PaymentMode { get; set; } = "Cash";
    public string? PaidTo { get; set; }
    public string? Notes { get; set; }
}

public class ClothExpenseSummaryDto
{
    public decimal TodayExpenses { get; set; }
    public decimal MonthExpenses { get; set; }
    public decimal TotalExpenses { get; set; }
    public int TotalExpensesCount { get; set; }
    public Dictionary<string, decimal> CategoryBreakdown { get; set; } = new();
}
