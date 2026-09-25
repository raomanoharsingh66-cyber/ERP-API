using BizFlow.Application.DTOs.Inventory;
using BizFlow.Application.Validators.Inventory;
using FluentAssertions;
using Xunit;

namespace BizFlow.UnitTests;

public class InventoryTests
{
    private readonly CreateProductValidator _productValidator = new();
    private readonly StockAdjustmentValidator _adjustmentValidator = new();
    private readonly StockTransferValidator _transferValidator = new();

    [Fact]
    public void CreateProductValidator_WithValidProduct_ShouldPass()
    {
        // Arrange
        var dto = new CreateProductDto
        {
            CategoryId = Guid.NewGuid(),
            UnitOfMeasureId = Guid.NewGuid(),
            SKU = "PROD-100",
            Name = "Stainless Steel Bolts M8",
            PurchasePrice = 25.50m,
            SellingPrice = 38.00m,
            TaxRate = 18.00m,
            MinStockLevel = 50m,
            MaxStockLevel = 1000m
        };

        // Act
        var result = _productValidator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateProductValidator_WithNegativePrices_ShouldFail()
    {
        // Arrange
        var dto = new CreateProductDto
        {
            CategoryId = Guid.NewGuid(),
            UnitOfMeasureId = Guid.NewGuid(),
            SKU = "PROD-200",
            Name = "Invalid Item",
            PurchasePrice = -10m,
            SellingPrice = -20m,
            TaxRate = 18.00m
        };

        // Act
        var result = _productValidator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PurchasePrice");
        result.Errors.Should().Contain(e => e.PropertyName == "SellingPrice");
    }

    [Fact]
    public void CreateProductValidator_WithEmptySKU_ShouldFail()
    {
        // Arrange
        var dto = new CreateProductDto
        {
            CategoryId = Guid.NewGuid(),
            UnitOfMeasureId = Guid.NewGuid(),
            SKU = "",
            Name = "Item Missing SKU",
            PurchasePrice = 10m,
            SellingPrice = 20m
        };

        // Act
        var result = _productValidator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SKU");
    }

    [Fact]
    public void StockAdjustmentValidator_WithZeroQuantity_ShouldFail()
    {
        // Arrange
        var dto = new StockAdjustmentDto
        {
            ProductId = Guid.NewGuid(),
            WarehouseId = Guid.NewGuid(),
            Quantity = 0m
        };

        // Act
        var result = _adjustmentValidator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Quantity");
    }

    [Fact]
    public void StockTransferValidator_WithSameSourceAndDestination_ShouldFail()
    {
        // Arrange
        var warehouseId = Guid.NewGuid();
        var dto = new StockTransferDto
        {
            ProductId = Guid.NewGuid(),
            FromWarehouseId = warehouseId,
            ToWarehouseId = warehouseId,
            Quantity = 25m
        };

        // Act
        var result = _transferValidator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ToWarehouseId");
    }
}
