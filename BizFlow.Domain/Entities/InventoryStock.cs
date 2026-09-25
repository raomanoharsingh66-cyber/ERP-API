using BizFlow.Domain.Common;

namespace BizFlow.Domain.Entities;

public class InventoryStock : BaseEntity, IBusinessScoped
{
    public Guid BusinessId { get; set; }
    public Guid ProductId { get; set; }
    public Guid WarehouseId { get; set; }

    public decimal QuantityOnHand { get; set; } = 0m;
    public decimal QuantityReserved { get; set; } = 0m;
    public decimal AverageCost { get; set; } = 0m;

    public decimal QuantityAvailable => Math.Max(0m, QuantityOnHand - QuantityReserved);

    // Navigation properties
    public Business Business { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public Warehouse Warehouse { get; set; } = null!;
}
