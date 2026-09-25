using System.ComponentModel.DataAnnotations;
using BizFlow.Domain.Enums;

namespace BizFlow.Application.DTOs.Purchases;

public class GoodsReceiptNoteItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductSKU { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public Guid? PurchaseOrderItemId { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal AcceptedQuantity { get; set; }
    public decimal RejectedQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? RejectionReason { get; set; }
    public string? Notes { get; set; }
}

public class GoodsReceiptNoteDto
{
    public Guid Id { get; set; }
    public string GRNNumber { get; set; } = string.Empty;
    public DateTime ReceiptDate { get; set; }

    public Guid? PurchaseOrderId { get; set; }
    public string? PONumber { get; set; }

    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;

    public string? SupplierDeliveryNoteNo { get; set; }
    public GRNStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public string? Notes { get; set; }

    public List<GoodsReceiptNoteItemDto> Items { get; set; } = new();
    public DateTimeOffset CreatedOn { get; set; }
}

public class CreateGoodsReceiptNoteItemDto
{
    [Required]
    public Guid ProductId { get; set; }

    public Guid? PurchaseOrderItemId { get; set; }

    [Range(0.001, 10000000, ErrorMessage = "Received quantity must be greater than zero.")]
    public decimal ReceivedQuantity { get; set; }

    [Range(0.0, 10000000)]
    public decimal AcceptedQuantity { get; set; }

    [Range(0.0, 10000000)]
    public decimal RejectedQuantity { get; set; } = 0;

    [Range(0.0, 10000000)]
    public decimal UnitPrice { get; set; } // Unit cost for WAC calculation

    public string? RejectionReason { get; set; }
    public string? Notes { get; set; }
}

public class CreateGoodsReceiptNoteDto
{
    public Guid? PurchaseOrderId { get; set; }

    [Required]
    public Guid SupplierId { get; set; }

    [Required]
    public Guid WarehouseId { get; set; }

    public DateTime ReceiptDate { get; set; } = DateTime.UtcNow;
    public string? SupplierDeliveryNoteNo { get; set; }
    public string? Notes { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "GRN must contain at least one item row.")]
    public List<CreateGoodsReceiptNoteItemDto> Items { get; set; } = new();
}
