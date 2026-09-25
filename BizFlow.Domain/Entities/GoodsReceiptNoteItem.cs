using BizFlow.Domain.Common;

namespace BizFlow.Domain.Entities;

public class GoodsReceiptNoteItem : BaseEntity
{
    public Guid GoodsReceiptNoteId { get; set; }
    public GoodsReceiptNote? GoodsReceiptNote { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public Guid? PurchaseOrderItemId { get; set; }
    public PurchaseOrderItem? PurchaseOrderItem { get; set; }

    public decimal ReceivedQuantity { get; set; }
    public decimal AcceptedQuantity { get; set; }
    public decimal RejectedQuantity { get; set; }
    public decimal UnitPrice { get; set; } // Unit price recorded on receipt for WAC recalculation
    public string? RejectionReason { get; set; }
    public string? Notes { get; set; }
}
