using BizFlow.Domain.Common;
using BizFlow.Domain.Enums;

namespace BizFlow.Domain.Entities;

public class GoodsReceiptNote : BaseEntity, IAuditableEntity, IBusinessScoped
{
    public Guid BusinessId { get; set; }
    public Business? Business { get; set; }

    public string GRNNumber { get; set; } = string.Empty;
    public DateTime ReceiptDate { get; set; }

    public Guid? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    public Guid SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public Guid WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    public string? SupplierDeliveryNoteNo { get; set; } // Vendor Delivery Challan / LR No
    public GRNStatus Status { get; set; } = GRNStatus.Received;
    public string? Notes { get; set; }

    public ICollection<GoodsReceiptNoteItem> Items { get; set; } = new List<GoodsReceiptNoteItem>();
}
