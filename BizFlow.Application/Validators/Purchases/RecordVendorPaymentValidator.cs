using BizFlow.Application.DTOs.Purchases;
using FluentValidation;

namespace BizFlow.Application.Validators.Purchases;

public class RecordVendorPaymentValidator : AbstractValidator<RecordVendorPaymentDto>
{
    public RecordVendorPaymentValidator()
    {
        RuleFor(x => x.PurchaseBillId)
            .NotEmpty().WithMessage("Vendor bill ID is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Disbursement amount must be greater than zero.");

        RuleFor(x => x.PaymentDate)
            .NotEmpty().WithMessage("Payment date is required.");
    }
}
