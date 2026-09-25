using BizFlow.Application.DTOs.Purchases;
using FluentValidation;

namespace BizFlow.Application.Validators.Purchases;

public class CreatePurchaseOrderValidator : AbstractValidator<CreatePurchaseOrderDto>
{
    public CreatePurchaseOrderValidator()
    {
        RuleFor(x => x.SupplierId)
            .NotEmpty().WithMessage("Supplier is required.");

        RuleFor(x => x.WarehouseId)
            .NotEmpty().WithMessage("Destination warehouse is required.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Purchase order must contain at least one item.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId)
                .NotEmpty().WithMessage("Product is required.");

            item.RuleFor(i => i.OrderedQuantity)
                .GreaterThan(0).WithMessage("Ordered quantity must be greater than zero.");

            item.RuleFor(i => i.UnitPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Unit price cannot be negative.");

            item.RuleFor(i => i.TaxRate)
                .InclusiveBetween(0, 100).WithMessage("Tax rate must be between 0 and 100.");
        });
    }
}
