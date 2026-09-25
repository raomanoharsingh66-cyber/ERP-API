using BizFlow.Application.DTOs.Sales;
using FluentValidation;

namespace BizFlow.Application.Validators.Sales;

public class RecordSalesPaymentValidator : AbstractValidator<RecordSalesPaymentDto>
{
    public RecordSalesPaymentValidator()
    {
        RuleFor(x => x.SalesInvoiceId)
            .NotEmpty().WithMessage("Sales invoice is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Payment amount must be greater than zero.");

        RuleFor(x => x.PaymentDate)
            .NotEmpty().WithMessage("Payment date is required.");
    }
}
