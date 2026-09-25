using BizFlow.Application.DTOs.Sales;
using FluentValidation;

namespace BizFlow.Application.Validators.Sales;

public class CreateSalesOrderValidator : AbstractValidator<CreateSalesOrderDto>
{
    public CreateSalesOrderValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer is required.");

        RuleFor(x => x.WarehouseId)
            .NotEmpty().WithMessage("Dispatch warehouse is required.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Order must contain at least one line item.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId)
                .NotEmpty().WithMessage("Product is required.");

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than zero.");

            item.RuleFor(i => i.UnitPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Unit price cannot be negative.");

            item.RuleFor(i => i.DiscountPercentage)
                .InclusiveBetween(0, 100).WithMessage("Discount percentage must be between 0 and 100.");

            item.RuleFor(i => i.TaxRate)
                .InclusiveBetween(0, 100).WithMessage("Tax rate must be between 0 and 100.");
        });
    }
}
