using BizFlow.Domain.Common;
using BizFlow.Domain.Enums;

namespace BizFlow.Domain.Entities;

public class PurchaseBill : BaseEntity, IAuditableEntity, IBusinessScoped
{
    public Guid BusinessId { get; set; }
    public Business? Business { get; set; }

    public string BillNumber { get; set; } = string.Empty;
    public string? VendorInvoiceNumber { get; set; } // Vendor's tax invoice reference
    public DateTime BillDate { get; set; }
    public DateTime DueDate { get; set; }

    public Guid? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    public Guid SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public Guid WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    public BillStatus Status { get; set; } = BillStatus.Posted;

    // Financials & Input Tax Credit (ITC) Breakdown
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }

    public string? Notes { get; set; }

    public ICollection<PurchaseBillItem> Items { get; set; } = new List<PurchaseBillItem>();
    public ICollection<VendorPayment> Payments { get; set; } = new List<VendorPayment>();
}
