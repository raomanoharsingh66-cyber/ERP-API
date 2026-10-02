using System;
using System.Collections.Generic;
using BizFlow.Domain.Common;

namespace BizFlow.Domain.Entities.ClothHub;

public class ClothSalesReturn : BaseEntity, IBusinessScoped
{
    public Guid BusinessId { get; set; }

    public string ReturnNumber { get; set; } = string.Empty; // e.g. "RET-20261002-0001" or "EXC-20261002-0001"
    public DateTime ReturnDate { get; set; } = DateTime.UtcNow;

    public Guid? ClothSalesInvoiceId { get; set; }
    public ClothSalesInvoice? SalesInvoice { get; set; }
    public string? OriginalInvoiceNumber { get; set; }

    public string CustomerName { get; set; } = "Walk-in Customer";
    public string? CustomerPhone { get; set; }

    // Type: "Exchange" or "Return"
    public string ReturnType { get; set; } = "Exchange";

    public decimal TotalReturnAmount { get; set; }    // Total value of garments returned
    public decimal TotalExchangeAmount { get; set; }  // Total value of new replacement garments taken
    public decimal NetDifference { get; set; }        // ExchangeAmount - ReturnAmount

    // Settlement: "Even", "Cash", "UPI", "CreditNote", "Card"
    public string SettlementMode { get; set; } = "Even";
    public string? CreditNoteNumber { get; set; }     // e.g. "CN-20261002-0001" if store credit issued

    // Reason: "Size Issue", "Colour Mismatch", "Defective Fabric", "Customer Changed Mind", "Fit Not Good"
    public string Reason { get; set; } = "Size Issue";
    public string? Remarks { get; set; }

    public string? HandledBy { get; set; }

    public ICollection<ClothSalesReturnItem> Items { get; set; } = new List<ClothSalesReturnItem>();
}

public class ClothSalesReturnItem : BaseEntity
{
    public Guid ClothSalesReturnId { get; set; }
    public ClothSalesReturn? SalesReturn { get; set; }

    // Action: "Return" (item coming back into store) or "Replacement" (item going out to customer)
    public string ItemAction { get; set; } = "Return";

    public Guid ClothProductVariantId { get; set; }
    public ClothProductVariant? Variant { get; set; }

    public string ItemDescription { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string SizeName { get; set; } = string.Empty;
    public string ColourName { get; set; } = string.Empty;

    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    // Condition: "Fresh / Resaleable" (added to showroom stock) or "Damaged / Defective" (quarantined)
    public string Condition { get; set; } = "Fresh / Resaleable";
}
