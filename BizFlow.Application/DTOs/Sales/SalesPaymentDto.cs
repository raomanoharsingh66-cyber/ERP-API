using System.ComponentModel.DataAnnotations;
using BizFlow.Domain.Enums;

namespace BizFlow.Application.DTOs.Sales;

public class SalesPaymentDto
{
    public Guid Id { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;
    public Guid SalesInvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public string MethodName => Method.ToString();
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
}

public class RecordSalesPaymentDto
{
    [Required]
    public Guid SalesInvoiceId { get; set; }

    [Range(0.01, 100000000, ErrorMessage = "Payment amount must be greater than zero.")]
    public decimal Amount { get; set; }

    public PaymentMethod Method { get; set; } = PaymentMethod.BankTransfer;

    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    public string? ReferenceNumber { get; set; } // UTR / Cheque / Txn ID

    public string? Notes { get; set; }
}
