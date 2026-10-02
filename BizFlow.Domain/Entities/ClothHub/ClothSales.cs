using System;
using System.Collections.Generic;
using BizFlow.Domain.Common;

namespace BizFlow.Domain.Entities.ClothHub;

public class ClothSalesInvoice : BaseEntity, IBusinessScoped
{
    public Guid BusinessId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty; // e.g. "POS-20261002-0001"
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

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

    // Payment modes: "Cash", "UPI", "Card", "Credit", "Split"
    public string PaymentMode { get; set; } = "Cash";
    public string? PaymentBreakdownJson { get; set; }

    // Status: "Completed", "Returned", "Exchanged", "Cancelled"
    public string Status { get; set; } = "Completed";

    public string? CashierName { get; set; }
    public string? Notes { get; set; }

    public ICollection<ClothSalesInvoiceItem> Items { get; set; } = new List<ClothSalesInvoiceItem>();
}

public class ClothSalesInvoiceItem : BaseEntity
{
    public Guid ClothSalesInvoiceId { get; set; }
    public ClothSalesInvoice? SalesInvoice { get; set; }

    public Guid ClothProductVariantId { get; set; }
    public ClothProductVariant? Variant { get; set; }

    public string ItemDescription { get; set; } = string.Empty; // e.g. "Men's Cotton Polo T-Shirt"
    public string Sku { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string SizeName { get; set; } = string.Empty;
    public string ColourName { get; set; } = string.Empty;

    public int Quantity { get; set; } = 1;
    public decimal Mrp { get; set; }
    public decimal UnitSellingPrice { get; set; }
    public decimal DiscountAmount { get; set; } = 0;
    
    public decimal GstRate { get; set; } = 5.0m;
    public decimal GstAmount { get; set; }
    public decimal LineTotal { get; set; }

    public bool IsExchangedOrReturned { get; set; } = false;
}
