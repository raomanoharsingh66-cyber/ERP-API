using BizFlow.Domain.Common;

namespace BizFlow.Domain.Entities;

public class PurchaseBillItem : BaseEntity
{
    public Guid PurchaseBillId { get; set; }
    public PurchaseBill? PurchaseBill { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public string? Notes { get; set; }
}
