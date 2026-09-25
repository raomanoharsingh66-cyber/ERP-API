using System.ComponentModel.DataAnnotations;
using BizFlow.Domain.Enums;

namespace BizFlow.Application.DTOs.Sales;

public class SalesInvoiceItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductSKU { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
}

public class SalesInvoiceDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }

    public Guid? SalesOrderId { get; set; }
    public string? OrderNumber { get; set; }

    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public string? CustomerGSTIN { get; set; }
    public string? CustomerBillingAddress { get; set; }
    public string? CustomerBillingCity { get; set; }
    public string? CustomerBillingState { get; set; }

    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;

    public InvoiceStatus Status { get; set; }
    public string StatusName => Status.ToString();

    // Financials
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }

    public string? Notes { get; set; }

    public List<SalesInvoiceItemDto> Items { get; set; } = new();
    public List<SalesPaymentDto> Payments { get; set; } = new();
    public DateTimeOffset CreatedOn { get; set; }
}

public class CreateSalesInvoiceItemDto
{
    [Required]
    public Guid ProductId { get; set; }

    [Range(0.001, 10000000, ErrorMessage = "Quantity must be greater than 0.")]
    public decimal Quantity { get; set; }

    [Range(0.0, 10000000, ErrorMessage = "Unit price cannot be negative.")]
    public decimal UnitPrice { get; set; }

    [Range(0.0, 100.0)]
    public decimal DiscountPercentage { get; set; } = 0;

    [Range(0.0, 100.0)]
    public decimal TaxRate { get; set; } = 18;

    public string? Notes { get; set; }
}

public class CreateSalesInvoiceDto
{
    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    public Guid WarehouseId { get; set; }

    public Guid? SalesOrderId { get; set; }

    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(30);

    public bool IsInterstate { get; set; } = false;

    public string? Notes { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one item is required on the invoice.")]
    public List<CreateSalesInvoiceItemDto> Items { get; set; } = new();
}

public class SalesSummaryDto
{
    public decimal TotalSales { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalOutstanding { get; set; }
    public decimal TotalOverdue { get; set; }
    public int TotalInvoicesCount { get; set; }
    public int PendingOrdersCount { get; set; }
    public int ActiveCustomersCount { get; set; }
}
