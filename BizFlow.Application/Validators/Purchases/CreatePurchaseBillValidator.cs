using BizFlow.Application.DTOs.Purchases;
using FluentValidation;

namespace BizFlow.Application.Validators.Purchases;

public class CreatePurchaseBillValidator : AbstractValidator<CreatePurchaseBillDto>
{
    public CreatePurchaseBillValidator()
    {
        RuleFor(x => x.SupplierId)
            .NotEmpty().WithMessage("Supplier is required.");

        RuleFor(x => x.WarehouseId)
            .NotEmpty().WithMessage("Warehouse is required.");

        RuleFor(x => x.DueDate)
            .GreaterThanOrEqualTo(x => x.BillDate)
            .WithMessage("Bill due date cannot be earlier than bill date.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Vendor bill must contain at least one line item.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId)
                .NotEmpty().WithMessage("Product is required.");

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than zero.");

            item.RuleFor(i => i.UnitPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Unit price cannot be negative.");

            item.RuleFor(i => i.TaxRate)
                .InclusiveBetween(0, 100).WithMessage("Tax rate must be between 0 and 100.");
        });
    }
}
