using BizFlow.Domain.Common;

namespace BizFlow.Domain.Entities;

public class Product : BaseEntity, IBusinessScoped
{
    public Guid BusinessId { get; set; }
    public Guid CategoryId { get; set; }
    public Guid UnitOfMeasureId { get; set; }

    public string SKU { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal TaxRate { get; set; } = 18.0m; // GST percentage
    public string? HSNCode { get; set; }

    public decimal MinStockLevel { get; set; } = 10m;
    public decimal MaxStockLevel { get; set; } = 1000m;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Business Business { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public UnitOfMeasure UnitOfMeasure { get; set; } = null!;
    public ICollection<InventoryStock> Stocks { get; set; } = new List<InventoryStock>();
    public ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
}
