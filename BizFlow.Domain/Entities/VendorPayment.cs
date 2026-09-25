using BizFlow.Domain.Common;
using BizFlow.Domain.Enums;

namespace BizFlow.Domain.Entities;

public class VendorPayment : BaseEntity, IAuditableEntity, IBusinessScoped
{
    public Guid BusinessId { get; set; }
    public Business? Business { get; set; }

    public string PaymentNumber { get; set; } = string.Empty;
    public Guid PurchaseBillId { get; set; }
    public PurchaseBill? PurchaseBill { get; set; }

    public Guid SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; } = PaymentMethod.BankTransfer;
    public string? ReferenceNumber { get; set; } // Transaction UTR / Cheque No
    public string? Notes { get; set; }
}
