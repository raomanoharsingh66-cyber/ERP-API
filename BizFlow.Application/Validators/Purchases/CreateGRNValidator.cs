using BizFlow.Application.DTOs.Purchases;
using FluentValidation;

namespace BizFlow.Application.Validators.Purchases;

public class CreateGRNValidator : AbstractValidator<CreateGoodsReceiptNoteDto>
{
    public CreateGRNValidator()
    {
        RuleFor(x => x.SupplierId)
            .NotEmpty().WithMessage("Supplier is required.");

        RuleFor(x => x.WarehouseId)
            .NotEmpty().WithMessage("Receiving warehouse is required.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("GRN must contain at least one line item.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId)
                .NotEmpty().WithMessage("Product is required.");

            item.RuleFor(i => i.ReceivedQuantity)
                .GreaterThan(0).WithMessage("Received quantity must be greater than zero.");

            item.RuleFor(i => i.AcceptedQuantity)
                .GreaterThanOrEqualTo(0).WithMessage("Accepted quantity cannot be negative.")
                .LessThanOrEqualTo(i => i.ReceivedQuantity).WithMessage("Accepted quantity cannot exceed received quantity.");

            item.RuleFor(i => i.RejectedQuantity)
                .GreaterThanOrEqualTo(0).WithMessage("Rejected quantity cannot be negative.");

            item.RuleFor(i => i.UnitPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Unit price cannot be negative.");
        });
    }
}
