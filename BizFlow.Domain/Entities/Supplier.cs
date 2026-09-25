using BizFlow.Domain.Common;

namespace BizFlow.Domain.Entities;

public class Supplier : BaseEntity, IAuditableEntity, IBusinessScoped
{
    public Guid BusinessId { get; set; }
    public Business? Business { get; set; }

    public string SupplierCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }

    // Tax Identifiers
    public string? GSTIN { get; set; }
    public string? PAN { get; set; }

    // Address
    public string? BillingAddress { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; } = "India";

    public int PaymentTermsDays { get; set; } = 30;
    public decimal OutstandingPayable { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }

    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    public ICollection<PurchaseBill> PurchaseBills { get; set; } = new List<PurchaseBill>();
}
