using System;
using System.Collections.Generic;

namespace BizFlow.Application.DTOs.ClothHub;

// --- POS FAST LOOKUP DTO ---

public class ClothPosLookupItemDto
{
    public Guid VariantId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string SizeName { get; set; } = string.Empty;
    public string ColourName { get; set; } = string.Empty;
    public string ColourHex { get; set; } = string.Empty;
    public decimal Mrp { get; set; }
    public decimal SellingPrice { get; set; }
    public int CurrentStock { get; set; }
    public decimal GstRate { get; set; } = 5.0m;
}

// --- SALES INVOICE DTOs ---

public class ClothSalesInvoiceDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string CustomerName { get; set; } = "Walk-in Customer";
    public string? CustomerPhone { get; set; }
    public string? CustomerGstin { get; set; }

    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal RoundOff { get; set; }
    public decimal GrandTotal { get; set; }

    public decimal PaidAmount { get; set; }
    public decimal ChangeReturned { get; set; }
    public string PaymentMode { get; set; } = "Cash";
    public string? PaymentBreakdownJson { get; set; }
    public string Status { get; set; } = "Completed";
    public string? CashierName { get; set; }
    public string? Notes { get; set; }

    public int TotalPieces { get; set; }
    public List<ClothSalesInvoiceItemDto> Items { get; set; } = new();
}

public class ClothSalesInvoiceItemDto
{
    public Guid Id { get; set; }
    public Guid ClothProductVariantId { get; set; }
    public string ItemDescription { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string SizeName { get; set; } = string.Empty;
    public string ColourName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Mrp { get; set; }
    public decimal UnitSellingPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal GstRate { get; set; }
    public decimal GstAmount { get; set; }
    public decimal LineTotal { get; set; }
    public bool IsExchangedOrReturned { get; set; }
}

public class CreateClothSalesInvoiceDto
{
    public string CustomerName { get; set; } = "Walk-in Customer";
    public string? CustomerPhone { get; set; }
    public string? CustomerGstin { get; set; }
    public decimal DiscountAmount { get; set; } = 0;
    public decimal PaidAmount { get; set; }
    public string PaymentMode { get; set; } = "Cash"; // Cash, UPI, Card, Credit, Split
    public string? PaymentBreakdownJson { get; set; }
    public string? Notes { get; set; }
    public List<CreateClothSalesInvoiceItemDto> Items { get; set; } = new();
}

public class CreateClothSalesInvoiceItemDto
{
    public Guid ClothProductVariantId { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal DiscountAmount { get; set; } = 0;
}

// --- SALES SUMMARY DTO ---

public class ClothSalesSummaryDto
{
    public decimal TodaySalesTotal { get; set; }
    public int TodayBillsCount { get; set; }
    public int TodayGarmentsSold { get; set; }
    public decimal TodayCashSales { get; set; }
    public decimal TodayUpiSales { get; set; }
    public decimal TodayCardSales { get; set; }
    public decimal LifetimeSalesTotal { get; set; }
    public int LifetimeBillsCount { get; set; }
}
