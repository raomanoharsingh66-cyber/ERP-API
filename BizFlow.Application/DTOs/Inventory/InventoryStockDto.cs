using BizFlow.Domain.Enums;

namespace BizFlow.Application.DTOs.Inventory;

public class InventoryStockDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductSKU { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitOfMeasureCode { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;

    public decimal QuantityOnHand { get; set; }
    public decimal QuantityReserved { get; set; }
    public decimal QuantityAvailable { get; set; }
    public decimal AverageCost { get; set; }
    public decimal TotalValue => QuantityOnHand * AverageCost;
}

public class StockAdjustmentDto
{
    public Guid ProductId { get; set; }
    public Guid WarehouseId { get; set; }
    public StockTransactionType Type { get; set; } = StockTransactionType.Adjustment; // Inward, Outward, Adjustment
    public decimal Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
}

public class StockTransferDto
{
    public Guid ProductId { get; set; }
    public Guid FromWarehouseId { get; set; }
    public Guid ToWarehouseId { get; set; }
    public decimal Quantity { get; set; }
    public string? Notes { get; set; }
}

public class StockTransactionDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductSKU { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public string? ReferenceType { get; set; }
    public string? ReferenceId { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
