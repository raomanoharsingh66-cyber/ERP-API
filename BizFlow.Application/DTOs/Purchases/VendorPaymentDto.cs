using System.ComponentModel.DataAnnotations;
using BizFlow.Domain.Enums;

namespace BizFlow.Application.DTOs.Purchases;

public class VendorPaymentDto
{
    public Guid Id { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;
    public Guid PurchaseBillId { get; set; }
    public string BillNumber { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public string MethodName => Method.ToString();
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
}

public class RecordVendorPaymentDto
{
    [Required]
    public Guid PurchaseBillId { get; set; }

    [Range(0.01, 100000000, ErrorMessage = "Disbursement amount must be greater than zero.")]
    public decimal Amount { get; set; }

    public PaymentMethod Method { get; set; } = PaymentMethod.BankTransfer;
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public string? ReferenceNumber { get; set; } // Bank UTR or Cheque No
    public string? Notes { get; set; }
}
