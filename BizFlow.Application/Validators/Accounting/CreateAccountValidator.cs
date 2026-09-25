using BizFlow.Application.DTOs.Accounting;
using FluentValidation;

namespace BizFlow.Application.Validators.Accounting;

public class CreateAccountValidator : AbstractValidator<CreateAccountDto>
{
    public CreateAccountValidator()
    {
        RuleFor(x => x.AccountCode)
            .NotEmpty().WithMessage("Account code is required.")
            .MaximumLength(20).WithMessage("Account code cannot exceed 20 characters.");

        RuleFor(x => x.AccountName)
            .NotEmpty().WithMessage("Account name is required.")
            .MaximumLength(150).WithMessage("Account name cannot exceed 150 characters.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("A valid account type (Asset, Liability, Equity, Revenue, Expense) is required.");
    }
}
