using System.ComponentModel.DataAnnotations;
using BizFlow.Domain.Enums;

namespace BizFlow.Application.DTOs.Purchases;

public class PurchaseOrderItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductSKU { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal OrderedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal PendingQuantity => Math.Max(0m, OrderedQuantity - ReceivedQuantity);
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
}

public class PurchaseOrderDto
{
    public Guid Id { get; set; }
    public string PONumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }

    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string SupplierCode { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;

    public PurchaseOrderStatus Status { get; set; }
    public string StatusName => Status.ToString();

    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }

    public List<PurchaseOrderItemDto> Items { get; set; } = new();
    public DateTimeOffset CreatedOn { get; set; }
}

public class CreatePurchaseOrderItemDto
{
    [Required]
    public Guid ProductId { get; set; }

    [Range(0.001, 10000000, ErrorMessage = "Ordered quantity must be greater than zero.")]
    public decimal OrderedQuantity { get; set; }

    [Range(0.0, 10000000, ErrorMessage = "Unit price cannot be negative.")]
    public decimal UnitPrice { get; set; }

    [Range(0.0, 100.0)]
    public decimal TaxRate { get; set; } = 18;

    public string? Notes { get; set; }
}

public class CreatePurchaseOrderDto
{
    [Required]
    public Guid SupplierId { get; set; }

    [Required]
    public Guid WarehouseId { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpectedDeliveryDate { get; set; }

    public string? Notes { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one item is required in the purchase order.")]
    public List<CreatePurchaseOrderItemDto> Items { get; set; } = new();
}

public class UpdatePurchaseOrderStatusDto
{
    [Required]
    public PurchaseOrderStatus Status { get; set; }
}
