using BizFlow.Application.DTOs.Purchases;
using FluentValidation;

namespace BizFlow.Application.Validators.Purchases;

public class CreateSupplierValidator : AbstractValidator<CreateSupplierDto>
{
    public CreateSupplierValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Supplier / Vendor name is required.")
            .MaximumLength(200).WithMessage("Vendor name cannot exceed 200 characters.");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("A valid email address is required.");

        RuleFor(x => x.GSTIN)
            .Matches(@"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$")
            .When(x => !string.IsNullOrEmpty(x.GSTIN))
            .WithMessage("Invalid GSTIN format (must be 15 alphanumeric characters).");

        RuleFor(x => x.PaymentTermsDays)
            .GreaterThanOrEqualTo(0).WithMessage("Payment terms in days cannot be negative.");
    }
}
