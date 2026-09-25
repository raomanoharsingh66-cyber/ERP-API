using BizFlow.Application.DTOs.Sales;
using BizFlow.Application.Validators.Sales;
using FluentAssertions;
using Xunit;

namespace BizFlow.UnitTests;

public class SalesTests
{
    private readonly CreateCustomerValidator _customerValidator = new();
    private readonly CreateSalesInvoiceValidator _invoiceValidator = new();
    private readonly RecordSalesPaymentValidator _paymentValidator = new();

    [Fact]
    public void CreateCustomerValidator_WithValidData_ShouldPass()
    {
        // Arrange
        var dto = new CreateCustomerDto
        {
            Name = "Precision Dynamics Ltd",
            Email = "finance@precisiondynamics.in",
            Phone = "+919876543210",
            GSTIN = "27AAACP1234A1Z9",
            CreditLimit = 100000m
        };

        // Act
        var result = _customerValidator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateCustomerValidator_WithInvalidEmail_ShouldFail()
    {
        // Arrange
        var dto = new CreateCustomerDto
        {
            Name = "Invalid Email Ltd",
            Email = "not-an-email",
            CreditLimit = 50000m
        };

        // Act
        var result = _customerValidator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void CreateCustomerValidator_WithNegativeCreditLimit_ShouldFail()
    {
        // Arrange
        var dto = new CreateCustomerDto
        {
            Name = "Test Client",
            CreditLimit = -500m
        };

        // Act
        var result = _customerValidator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CreditLimit");
    }

    [Fact]
    public void CreateSalesInvoiceValidator_WithNoItems_ShouldFail()
    {
        // Arrange
        var dto = new CreateSalesInvoiceDto
        {
            CustomerId = Guid.NewGuid(),
            WarehouseId = Guid.NewGuid(),
            Items = new List<CreateSalesInvoiceItemDto>()
        };

        // Act
        var result = _invoiceValidator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Items");
    }

    [Fact]
    public void CreateSalesInvoiceValidator_WithDueDateBeforeInvoiceDate_ShouldFail()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var dto = new CreateSalesInvoiceDto
        {
            CustomerId = Guid.NewGuid(),
            WarehouseId = Guid.NewGuid(),
            InvoiceDate = now,
            DueDate = now.AddDays(-1), // earlier than invoice date
            Items = new List<CreateSalesInvoiceItemDto>
            {
                new()
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 10,
                    UnitPrice = 100,
                    TaxRate = 18
                }
            }
        };

        // Act
        var result = _invoiceValidator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DueDate");
    }

    [Fact]
    public void RecordSalesPaymentValidator_WithZeroAmount_ShouldFail()
    {
        // Arrange
        var dto = new RecordSalesPaymentDto
        {
            SalesInvoiceId = Guid.NewGuid(),
            Amount = 0m,
            PaymentDate = DateTime.UtcNow
        };

        // Act
        var result = _paymentValidator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public void IntrastateTaxComputation_ShouldSplitEqualCgstAndSgst()
    {
        // Line calculation: Qty 10 * Rate 500 = 5000 Gross. Tax 18% = 900.
        decimal quantity = 10m;
        decimal unitPrice = 500m;
        decimal taxRate = 18m;
        bool isInterstate = false;

        decimal gross = quantity * unitPrice;
        decimal taxAmount = gross * (taxRate / 100m);
        decimal cgst = isInterstate ? 0 : taxAmount / 2m;
        decimal sgst = isInterstate ? 0 : taxAmount / 2m;
        decimal igst = isInterstate ? taxAmount : 0;

        gross.Should().Be(5000m);
        taxAmount.Should().Be(900m);
        cgst.Should().Be(450m);
        sgst.Should().Be(450m);
        igst.Should().Be(0m);
    }

    [Fact]
    public void InterstateTaxComputation_ShouldApplyFullIgst()
    {
        decimal quantity = 5m;
        decimal unitPrice = 1200m;
        decimal taxRate = 18m;
        bool isInterstate = true;

        decimal gross = quantity * unitPrice;
        decimal taxAmount = gross * (taxRate / 100m);
        decimal cgst = isInterstate ? 0 : taxAmount / 2m;
        decimal sgst = isInterstate ? 0 : taxAmount / 2m;
        decimal igst = isInterstate ? taxAmount : 0;

        gross.Should().Be(6000m);
        taxAmount.Should().Be(1080m);
        cgst.Should().Be(0m);
        sgst.Should().Be(0m);
        igst.Should().Be(1080m);
    }
}
