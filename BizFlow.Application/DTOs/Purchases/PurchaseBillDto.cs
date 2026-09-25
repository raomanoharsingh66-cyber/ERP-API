using System.ComponentModel.DataAnnotations;
using BizFlow.Domain.Enums;

namespace BizFlow.Application.DTOs.Purchases;

public class PurchaseBillItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductSKU { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
}

public class PurchaseBillDto
{
    public Guid Id { get; set; }
    public string BillNumber { get; set; } = string.Empty;
    public string? VendorInvoiceNumber { get; set; }
    public DateTime BillDate { get; set; }
    public DateTime DueDate { get; set; }

    public Guid? PurchaseOrderId { get; set; }
    public string? PONumber { get; set; }

    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string SupplierCode { get; set; } = string.Empty;
    public string? SupplierGSTIN { get; set; }

    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;

    public BillStatus Status { get; set; }
    public string StatusName => Status.ToString();

    // Financials
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public string? Notes { get; set; }

    public List<PurchaseBillItemDto> Items { get; set; } = new();
    public List<VendorPaymentDto> Payments { get; set; } = new();
    public DateTimeOffset CreatedOn { get; set; }
}

public class CreatePurchaseBillItemDto
{
    [Required]
    public Guid ProductId { get; set; }

    [Range(0.001, 10000000, ErrorMessage = "Quantity must be greater than zero.")]
    public decimal Quantity { get; set; }

    [Range(0.0, 10000000, ErrorMessage = "Unit price cannot be negative.")]
    public decimal UnitPrice { get; set; }

    [Range(0.0, 100.0)]
    public decimal TaxRate { get; set; } = 18;

    public string? Notes { get; set; }
}

public class CreatePurchaseBillDto
{
    [Required]
    public Guid SupplierId { get; set; }

    [Required]
    public Guid WarehouseId { get; set; }

    public Guid? PurchaseOrderId { get; set; }

    public string? VendorInvoiceNumber { get; set; }
    public DateTime BillDate { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(30);

    public bool IsInterstate { get; set; } = false;
    public string? Notes { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Vendor bill must contain at least one line item.")]
    public List<CreatePurchaseBillItemDto> Items { get; set; } = new();
}

public class PurchaseSummaryDto
{
    public decimal TotalPurchases { get; set; }
    public decimal TotalDisbursed { get; set; }
    public decimal TotalOutstandingPayables { get; set; }
    public decimal TotalOverduePayables { get; set; }
    public int TotalBillsCount { get; set; }
    public int PendingOrdersCount { get; set; }
    public int ActiveSuppliersCount { get; set; }
}
