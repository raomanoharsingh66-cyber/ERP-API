using BizFlow.Domain.Common;

namespace BizFlow.Domain.Entities;

public class Customer : BaseEntity, IAuditableEntity, IBusinessScoped
{
    public Guid BusinessId { get; set; }
    public Business? Business { get; set; }

    public string CustomerCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    
    // Tax Identifiers (India / Global)
    public string? GSTIN { get; set; }
    public string? PAN { get; set; }

    // Billing Address
    public string? BillingAddress { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingState { get; set; }
    public string? BillingPostalCode { get; set; }
    public string? BillingCountry { get; set; } = "India";

    // Shipping Address
    public string? ShippingAddress { get; set; }
    public string? ShippingCity { get; set; }
    public string? ShippingState { get; set; }
    public string? ShippingPostalCode { get; set; }
    public string? ShippingCountry { get; set; } = "India";

    // Commercial Terms
    public decimal CreditLimit { get; set; }
    public decimal OutstandingBalance { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }

    public ICollection<SalesOrder> Orders { get; set; } = new List<SalesOrder>();
    public ICollection<SalesInvoice> Invoices { get; set; } = new List<SalesInvoice>();
    public ICollection<SalesPayment> Payments { get; set; } = new List<SalesPayment>();
}
