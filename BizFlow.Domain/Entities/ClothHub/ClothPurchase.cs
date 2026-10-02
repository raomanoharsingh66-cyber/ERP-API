using System;
using System.Collections.Generic;
using BizFlow.Domain.Common;

namespace BizFlow.Domain.Entities.ClothHub;

public class ClothSupplier : BaseEntity, IBusinessScoped
{
    public Guid BusinessId { get; set; }

    public string SupplierName { get; set; } = string.Empty; // e.g. "Raymond Fabrics Mill"
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Gstin { get; set; } // 15-digit GST identification number

    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Pincode { get; set; }

    // Payment terms: "Net 30 Days", "Advance", "Cash on Delivery", "Net 15 Days", "Net 45 Days"
    public string PaymentTerms { get; set; } = "Net 30 Days";
    public decimal CreditLimit { get; set; } = 500000;
    public decimal CurrentPayableBalance { get; set; } = 0; // Total outstanding owed to this supplier
    public bool IsActive { get; set; } = true;

    public ICollection<ClothPurchaseBill> Bills { get; set; } = new List<ClothPurchaseBill>();
}

public class ClothPurchaseBill : BaseEntity, IBusinessScoped
{
    public Guid BusinessId { get; set; }

    public Guid ClothSupplierId { get; set; }
    public ClothSupplier? Supplier { get; set; }

    public string BillNumber { get; set; } = string.Empty; // Internal ERP bill e.g. "PB-2026-0001"
    public string SupplierInvoiceNumber { get; set; } = string.Empty; // Mill's tax invoice number
    public DateTime BillDate { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }

    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal RoundOff { get; set; }
    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; } = 0;
    public decimal BalanceAmount { get; set; } = 0;

    // Status: "Unpaid", "PartiallyPaid", "Paid"
    public string PaymentStatus { get; set; } = "Unpaid";

    // Inward status: "Received" (stock added), "Pending"
    public string InwardStatus { get; set; } = "Received";

    public string? Notes { get; set; }

    public ICollection<ClothPurchaseBillItem> Items { get; set; } = new List<ClothPurchaseBillItem>();
}

public class ClothPurchaseBillItem : BaseEntity
{
    public Guid ClothPurchaseBillId { get; set; }
    public ClothPurchaseBill? PurchaseBill { get; set; }

    // "VariantLoose" or "BoxPack"
    public string ItemType { get; set; } = "VariantLoose";

    public Guid? ClothProductVariantId { get; set; }
    public ClothProductVariant? Variant { get; set; }

    public Guid? ClothBoxPackId { get; set; }
    public ClothBoxPack? BoxPack { get; set; }

    public string ItemDescription { get; set; } = string.Empty; // e.g. "Raymond Linen Shirt - L - White"
    public int Quantity { get; set; } = 1;
    public decimal UnitRate { get; set; }
    public decimal GstRate { get; set; } = 5.0m;
    public decimal GstAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineTotal { get; set; }
}

public class ClothPurchaseReturn : BaseEntity, IBusinessScoped
{
    public Guid BusinessId { get; set; }

    public Guid ClothSupplierId { get; set; }
    public ClothSupplier? Supplier { get; set; }

    public Guid? ClothPurchaseBillId { get; set; }
    public ClothPurchaseBill? OriginalBill { get; set; }

    public string DebitNoteNumber { get; set; } = string.Empty; // e.g. "DN-2026-0001"
    public DateTime ReturnDate { get; set; } = DateTime.UtcNow;

    public int TotalQuantity { get; set; }
    public decimal TotalReturnAmount { get; set; }

    // Reasons: "Damaged / Defective Garment", "Fabric Weave Tear", "Incorrect Size Mix", "Colour Bleeding / Stains", "Excess Shipment"
    public string Reason { get; set; } = "Damaged / Defective Garment";
    public string? Remarks { get; set; }

    public ICollection<ClothPurchaseReturnItem> Items { get; set; } = new List<ClothPurchaseReturnItem>();
}

public class ClothPurchaseReturnItem : BaseEntity
{
    public Guid ClothPurchaseReturnId { get; set; }
    public ClothPurchaseReturn? PurchaseReturn { get; set; }

    public Guid? ClothProductVariantId { get; set; }
    public ClothProductVariant? Variant { get; set; }

    public Guid? ClothBoxPackId { get; set; }
    public ClothBoxPack? BoxPack { get; set; }

    public string ItemDescription { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitRate { get; set; }
    public decimal TotalAmount { get; set; }
}
