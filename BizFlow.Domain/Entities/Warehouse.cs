using BizFlow.Domain.Common;

namespace BizFlow.Domain.Entities;

public class Warehouse : BaseEntity, IBusinessScoped
{
    public Guid BusinessId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public bool IsDefault { get; set; } = false;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Business Business { get; set; } = null!;
    public ICollection<InventoryStock> Stocks { get; set; } = new List<InventoryStock>();
    public ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
}
