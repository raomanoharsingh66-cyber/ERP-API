using BizFlow.Domain.Common;
using BizFlow.Domain.Enums;

namespace BizFlow.Domain.Entities;

public class StockTransaction : BaseEntity, IBusinessScoped
{
    public Guid BusinessId { get; set; }
    public Guid ProductId { get; set; }
    public Guid WarehouseId { get; set; }

    public StockTransactionType TransactionType { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }

    public string? ReferenceType { get; set; } // "Adjustment", "Purchase", "Sale", "Transfer"
    public string? ReferenceId { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public Business Business { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public Warehouse Warehouse { get; set; } = null!;
}
