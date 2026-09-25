using BizFlow.Application.DTOs.Auth;
using FluentValidation;

namespace BizFlow.Application.Validators.Auth;

public class RegisterBusinessValidator : AbstractValidator<RegisterBusinessDto>
{
    public RegisterBusinessValidator()
    {
        RuleFor(x => x.BusinessName)
            .NotEmpty().WithMessage("Business Name is required.")
            .MaximumLength(200).WithMessage("Business Name cannot exceed 200 characters.");

        RuleFor(x => x.BusinessCode)
            .NotEmpty().WithMessage("Business Code is required.")
            .MaximumLength(50).WithMessage("Business Code cannot exceed 50 characters.")
            .Matches("^[A-Za-z0-9_-]+$").WithMessage("Business Code can only contain letters, numbers, hyphens, and underscores.");

        RuleFor(x => x.BusinessEmail)
            .NotEmpty().WithMessage("Business Email is required.")
            .EmailAddress().WithMessage("Business Email is not a valid email address.");

        RuleFor(x => x.AdminFirstName)
            .NotEmpty().WithMessage("Admin First Name is required.")
            .MaximumLength(100);

        RuleFor(x => x.AdminLastName)
            .NotEmpty().WithMessage("Admin Last Name is required.")
            .MaximumLength(100);

        RuleFor(x => x.AdminEmail)
            .NotEmpty().WithMessage("Admin Email is required.")
            .EmailAddress().WithMessage("Admin Email is not a valid email address.");

        RuleFor(x => x.AdminPassword)
            .NotEmpty().WithMessage("Admin Password is required.")
            .MinimumLength(8).WithMessage("Admin Password must be at least 8 characters long.")
            .Matches("[A-Z]").WithMessage("Admin Password must contain at least one uppercase letter.")
            .Matches("[0-9]").WithMessage("Admin Password must contain at least one number.");
    }
}
