using System.ComponentModel.DataAnnotations;
using BizFlow.Domain.Enums;

namespace BizFlow.Application.DTOs.Sales;

public class SalesOrderItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductSKU { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
}

public class SalesOrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }

    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;

    public OrderStatus Status { get; set; }
    public string StatusName => Status.ToString();

    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }

    public List<SalesOrderItemDto> Items { get; set; } = new();
    public DateTimeOffset CreatedOn { get; set; }
}

public class CreateSalesOrderItemDto
{
    [Required]
    public Guid ProductId { get; set; }

    [Range(0.001, 10000000, ErrorMessage = "Quantity must be greater than 0.")]
    public decimal Quantity { get; set; }

    [Range(0.0, 10000000, ErrorMessage = "Unit price cannot be negative.")]
    public decimal UnitPrice { get; set; }

    [Range(0.0, 100.0)]
    public decimal DiscountPercentage { get; set; } = 0;

    [Range(0.0, 100.0)]
    public decimal TaxRate { get; set; } = 18;

    public string? Notes { get; set; }
}

public class CreateSalesOrderDto
{
    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    public Guid WarehouseId { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpectedDeliveryDate { get; set; }

    public string? Notes { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one item is required in the sales order.")]
    public List<CreateSalesOrderItemDto> Items { get; set; } = new();
}

public class UpdateOrderStatusDto
{
    [Required]
    public OrderStatus Status { get; set; }
}
