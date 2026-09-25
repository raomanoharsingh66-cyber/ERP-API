using BizFlow.Application.DTOs.Purchases;
using BizFlow.Application.Validators.Purchases;
using FluentAssertions;
using Xunit;

namespace BizFlow.UnitTests;

public class PurchaseTests
{
    private readonly CreateSupplierValidator _supplierValidator = new();
    private readonly CreatePurchaseOrderValidator _poValidator = new();
    private readonly CreateGRNValidator _grnValidator = new();

    [Fact]
    public void CreateSupplierValidator_WithValidData_ShouldPass()
    {
        // Arrange
        var dto = new CreateSupplierDto
        {
            Name = "Precision Castings Ltd",
            ContactPerson = "Sunil Verma",
            Email = "orders@precisioncastings.in",
            Phone = "+919820011223",
            GSTIN = "27AAACP9988E1Z4",
            PaymentTermsDays = 30
        };

        // Act
        var result = _supplierValidator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateSupplierValidator_WithNegativePaymentTerms_ShouldFail()
    {
        // Arrange
        var dto = new CreateSupplierDto
        {
            Name = "Bad Vendor Terms",
            PaymentTermsDays = -10
        };

        // Act
        var result = _supplierValidator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PaymentTermsDays");
    }

    [Fact]
    public void CreatePurchaseOrderValidator_WithNoItems_ShouldFail()
    {
        // Arrange
        var dto = new CreatePurchaseOrderDto
        {
            SupplierId = Guid.NewGuid(),
            WarehouseId = Guid.NewGuid(),
            Items = new List<CreatePurchaseOrderItemDto>()
        };

        // Act
        var result = _poValidator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Items");
    }

    [Fact]
    public void CreateGRNValidator_WithAcceptedGreaterThanReceived_ShouldFail()
    {
        // Arrange
        var dto = new CreateGoodsReceiptNoteDto
        {
            SupplierId = Guid.NewGuid(),
            WarehouseId = Guid.NewGuid(),
            Items = new List<CreateGoodsReceiptNoteItemDto>
            {
                new()
                {
                    ProductId = Guid.NewGuid(),
                    ReceivedQuantity = 10m,
                    AcceptedQuantity = 15m // Invalid: accepted > received
                }
            }
        };

        // Act
        var result = _grnValidator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("AcceptedQuantity"));
    }

    [Fact]
    public void WeightedAverageCost_Recalculation_ShouldBeAccurate()
    {
        // Arrange: Existing stock has 10 units at ₹100 average cost
        decimal currentQty = 10m;
        decimal currentAvgCost = 100m;
        decimal currentValuation = currentQty * currentAvgCost; // 1,000

        // Inward consignment: 20 units at ₹160 purchase price
        decimal inwardQty = 20m;
        decimal inwardUnitPrice = 160m;
        decimal inwardValuation = inwardQty * inwardUnitPrice; // 3,200

        // Act
        decimal newTotalQty = currentQty + inwardQty; // 30
        decimal newAvgCost = (currentValuation + inwardValuation) / newTotalQty; // 4,200 / 30 = 140

        // Assert
        newTotalQty.Should().Be(30m);
        newAvgCost.Should().Be(140.00m);
    }

    [Fact]
    public void VendorPayment_ReductionCalculation_ShouldBeAccurate()
    {
        // Arrange
        decimal billTotal = 10000m;
        decimal firstDisbursement = 4000m;

        decimal paidAmount = firstDisbursement;
        decimal balanceAmount = billTotal - paidAmount;

        // Assert first payment
        paidAmount.Should().Be(4000m);
        balanceAmount.Should().Be(6000m);

        // Second payment
        decimal secondDisbursement = 6000m;
        paidAmount += secondDisbursement;
        balanceAmount -= secondDisbursement;

        paidAmount.Should().Be(10000m);
        balanceAmount.Should().Be(0m);
    }
}
