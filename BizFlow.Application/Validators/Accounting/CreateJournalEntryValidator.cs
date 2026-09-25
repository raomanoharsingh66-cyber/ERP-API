using BizFlow.Application.DTOs.Accounting;
using FluentValidation;

namespace BizFlow.Application.Validators.Accounting;

public class CreateJournalEntryValidator : AbstractValidator<CreateJournalEntryDto>
{
    public CreateJournalEntryValidator()
    {
        RuleFor(x => x.Narration)
            .NotEmpty().WithMessage("Journal entry narration / description is required.")
            .MaximumLength(500).WithMessage("Narration cannot exceed 500 characters.");

        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("A journal entry must contain line items.")
            .Must(lines => lines != null && lines.Count >= 2)
            .WithMessage("A double-entry journal voucher must contain at least 2 line items.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.AccountId)
                .NotEmpty().WithMessage("Account ID is required for each journal line.");

            line.RuleFor(l => l.Debit)
                .GreaterThanOrEqualTo(0).WithMessage("Debit amount cannot be negative.");

            line.RuleFor(l => l.Credit)
                .GreaterThanOrEqualTo(0).WithMessage("Credit amount cannot be negative.");

            line.RuleFor(l => l)
                .Must(l => (l.Debit > 0 && l.Credit == 0) || (l.Credit > 0 && l.Debit == 0))
                .WithMessage("Each line must contain either a debit or a credit amount, but not both or zero.");
        });

        RuleFor(x => x)
            .Must(x =>
            {
                if (x.Lines == null || x.Lines.Count == 0) return false;
                var totalDebit = x.Lines.Sum(l => l.Debit);
                var totalCredit = x.Lines.Sum(l => l.Credit);
                return Math.Abs(totalDebit - totalCredit) < 0.01m && totalDebit > 0;
            })
            .WithMessage("Strict Double-Entry Rule: Total Debits must equal Total Credits.");
    }
}
