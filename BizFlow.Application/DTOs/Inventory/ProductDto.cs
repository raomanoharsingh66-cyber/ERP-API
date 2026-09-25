namespace BizFlow.Application.DTOs.Inventory;

public class ProductDto
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public Guid UnitOfMeasureId { get; set; }
    public string UnitOfMeasureCode { get; set; } = string.Empty;

    public string SKU { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal TaxRate { get; set; }
    public string? HSNCode { get; set; }

    public decimal MinStockLevel { get; set; }
    public decimal MaxStockLevel { get; set; }
    public bool IsActive { get; set; }

    public decimal TotalStockQuantity { get; set; }
    public decimal TotalStockAvailable { get; set; }
    public bool IsLowStock => TotalStockQuantity <= MinStockLevel;
    public List<InventoryStockDto> WarehouseStocks { get; set; } = new();
}

public class ProductListItemDto
{
    public Guid Id { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string UnitOfMeasureCode { get; set; } = string.Empty;
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal TaxRate { get; set; }
    public decimal MinStockLevel { get; set; }
    public decimal TotalStockQuantity { get; set; }
    public decimal TotalStockAvailable { get; set; }
    public bool IsLowStock => TotalStockQuantity <= MinStockLevel;
    public bool IsActive { get; set; }
}

public class CreateProductDto
{
    public Guid CategoryId { get; set; }
    public Guid UnitOfMeasureId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal TaxRate { get; set; } = 18.0m;
    public string? HSNCode { get; set; }
    public decimal MinStockLevel { get; set; } = 10m;
    public decimal MaxStockLevel { get; set; } = 1000m;

    // Optional initial stock
    public Guid? InitialWarehouseId { get; set; }
    public decimal InitialQuantity { get; set; } = 0m;
}

public class UpdateProductDto
{
    public Guid CategoryId { get; set; }
    public Guid UnitOfMeasureId { get; set; }
    public string? Barcode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal TaxRate { get; set; }
    public string? HSNCode { get; set; }
    public decimal MinStockLevel { get; set; }
    public decimal MaxStockLevel { get; set; }
    public bool IsActive { get; set; }
}

public class ProductFilterDto
{
    public string? Search { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? WarehouseId { get; set; }
    public bool? IsLowStockOnly { get; set; }
    public bool? IsActiveOnly { get; set; } = true;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
