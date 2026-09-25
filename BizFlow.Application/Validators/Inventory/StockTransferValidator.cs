using BizFlow.Application.DTOs.Inventory;
using FluentValidation;

namespace BizFlow.Application.Validators.Inventory;

public class StockTransferValidator : AbstractValidator<StockTransferDto>
{
    public StockTransferValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required.");

        RuleFor(x => x.FromWarehouseId)
            .NotEmpty().WithMessage("Source Warehouse is required.");

        RuleFor(x => x.ToWarehouseId)
            .NotEmpty().WithMessage("Destination Warehouse is required.")
            .Must((dto, toId) => toId != dto.FromWarehouseId)
            .WithMessage("Source and Destination warehouses cannot be identical.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Transfer quantity must be greater than zero.");
    }
}
