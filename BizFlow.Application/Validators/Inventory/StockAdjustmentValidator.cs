using BizFlow.Application.DTOs.Inventory;
using FluentValidation;

namespace BizFlow.Application.Validators.Inventory;

public class StockAdjustmentValidator : AbstractValidator<StockAdjustmentDto>
{
    public StockAdjustmentValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required.");

        RuleFor(x => x.WarehouseId)
            .NotEmpty().WithMessage("Warehouse ID is required.");

        RuleFor(x => x.Quantity)
            .NotEqual(0).WithMessage("Adjustment quantity cannot be zero.");
    }
}
