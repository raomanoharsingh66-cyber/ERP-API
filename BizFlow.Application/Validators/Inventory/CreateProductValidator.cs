using BizFlow.Application.DTOs.Inventory;
using FluentValidation;

namespace BizFlow.Application.Validators.Inventory;

public class CreateProductValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product Name is required.")
            .MaximumLength(200).WithMessage("Product Name cannot exceed 200 characters.");

        RuleFor(x => x.SKU)
            .NotEmpty().WithMessage("SKU is required.")
            .MaximumLength(50).WithMessage("SKU cannot exceed 50 characters.")
            .Matches("^[A-Za-z0-9_.-]+$").WithMessage("SKU can only contain alphanumeric characters, hyphens, and underscores.");

        // Category and UnitOfMeasure have smart fallback defaults in service layer if not explicitly selected

        RuleFor(x => x.PurchasePrice)
            .GreaterThanOrEqualTo(0).WithMessage("Purchase Price cannot be negative.");

        RuleFor(x => x.SellingPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Selling Price cannot be negative.");

        RuleFor(x => x.TaxRate)
            .InclusiveBetween(0, 100).WithMessage("Tax rate must be between 0% and 100%.");

        RuleFor(x => x.MinStockLevel)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum Stock Level cannot be negative.");
    }
}
