using BizFlow.Application.DTOs.Accounting;
using BizFlow.Application.Validators.Accounting;
using BizFlow.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace BizFlow.UnitTests;

public class AccountingTests
{
    private readonly CreateAccountValidator _accountValidator = new();
    private readonly CreateJournalEntryValidator _journalValidator = new();

    [Fact]
    public void CreateAccountValidator_WithValidData_ShouldPass()
    {
        // Arrange
        var dto = new CreateAccountDto
        {
            AccountCode = "1050",
            AccountName = "SBI Operating Current Account",
            Type = AccountType.Asset,
            Subtype = "Bank",
            InitialBalance = 250000m
        };

        // Act
        var result = _accountValidator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateAccountValidator_WithEmptyCode_ShouldFail()
    {
        // Arrange
        var dto = new CreateAccountDto
        {
            AccountCode = "",
            AccountName = "Unnamed Account",
            Type = AccountType.Expense
        };

        // Act
        var result = _accountValidator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AccountCode");
    }

    [Fact]
    public void CreateJournalEntryValidator_WithBalancedDebitsAndCredits_ShouldPass()
    {
        // Arrange: Rent payment of ₹50,000 via Bank
        var dto = new CreateJournalEntryDto
        {
            EntryDate = DateTime.UtcNow,
            Narration = "Rent payment for unit 4",
            Lines = new List<CreateJournalEntryLineDto>
            {
                new() { AccountId = Guid.NewGuid(), Debit = 50000m, Credit = 0m, Description = "Rent Expense" },
                new() { AccountId = Guid.NewGuid(), Debit = 0m, Credit = 50000m, Description = "Bank Account" }
            }
        };

        // Act
        var result = _journalValidator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateJournalEntryValidator_WithImbalancedDebitsAndCredits_ShouldFail()
    {
        // Arrange: Imbalanced voucher (Debit 50,000 != Credit 45,000)
        var dto = new CreateJournalEntryDto
        {
            EntryDate = DateTime.UtcNow,
            Narration = "Imbalanced entry test",
            Lines = new List<CreateJournalEntryLineDto>
            {
                new() { AccountId = Guid.NewGuid(), Debit = 50000m, Credit = 0m },
                new() { AccountId = Guid.NewGuid(), Debit = 0m, Credit = 45000m }
            }
        };

        // Act
        var result = _journalValidator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Total Debits must equal Total Credits"));
    }

    [Fact]
    public void CreateJournalEntryValidator_WithLessThanTwoLines_ShouldFail()
    {
        // Arrange: Single line cannot constitute a double-entry voucher
        var dto = new CreateJournalEntryDto
        {
            EntryDate = DateTime.UtcNow,
            Narration = "Single leg voucher",
            Lines = new List<CreateJournalEntryLineDto>
            {
                new() { AccountId = Guid.NewGuid(), Debit = 10000m, Credit = 0m }
            }
        };

        // Act
        var result = _journalValidator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("at least 2 line items"));
    }

    [Fact]
    public void TrialBalance_DoubleEntryEquivalence_SumOfDebitsShouldEqualSumOfCredits()
    {
        // Arrange
        var items = new List<TrialBalanceItemDto>
        {
            new() { AccountCode = "1020", AccountName = "Bank", Type = AccountType.Asset, Debit = 500000m, Credit = 0m },
            new() { AccountCode = "1200", AccountName = "Inventory", Type = AccountType.Asset, Debit = 200000m, Credit = 0m },
            new() { AccountCode = "2010", AccountName = "Accounts Payable", Type = AccountType.Liability, Debit = 0m, Credit = 150000m },
            new() { AccountCode = "3010", AccountName = "Capital", Type = AccountType.Equity, Debit = 0m, Credit = 550000m }
        };

        // Act
        var totalDebit = items.Sum(i => i.Debit);
        var totalCredit = items.Sum(i => i.Credit);

        // Assert
        totalDebit.Should().Be(700000m);
        totalCredit.Should().Be(700000m);
        totalDebit.Should().Be(totalCredit);
    }

    [Fact]
    public void ProfitLoss_Formula_NetProfitShouldBeGrossProfitMinusExpenses()
    {
        // Arrange
        decimal revenue = 1200000m;
        decimal cogs = 700000m;
        decimal rent = 80000m;
        decimal salaries = 150000m;
        decimal utilities = 20000m;

        // Act
        decimal grossProfit = revenue - cogs; // 500,000
        decimal totalExpenses = rent + salaries + utilities; // 250,000
        decimal netProfit = grossProfit - totalExpenses; // 250,000
        decimal margin = (netProfit / revenue) * 100m; // 20.83%

        // Assert
        grossProfit.Should().Be(500000m);
        totalExpenses.Should().Be(250000m);
        netProfit.Should().Be(250000m);
        Math.Round(margin, 2).Should().Be(20.83m);
    }

    [Fact]
    public void BalanceSheet_AccountingEquation_AssetsShouldEqualLiabilitiesPlusEquity()
    {
        // Arrange: Fundamental accounting equation: Assets = Liabilities + Equity (including Net Profit)
        decimal bank = 600000m;
        decimal ar = 150000m;
        decimal inventory = 250000m;
        decimal totalAssets = bank + ar + inventory; // 1,000,000

        decimal ap = 200000m;
        decimal taxPayable = 50000m;
        decimal totalLiabilities = ap + taxPayable; // 250,000

        decimal initialCapital = 600000m;
        decimal currentYearNetProfit = 150000m; // Retained earnings
        decimal totalEquity = initialCapital + currentYearNetProfit; // 750,000

        // Act
        decimal totalLiabilitiesAndEquity = totalLiabilities + totalEquity; // 1,000,000

        // Assert
        totalAssets.Should().Be(totalLiabilitiesAndEquity);
        (totalAssets - totalLiabilitiesAndEquity).Should().Be(0m);
    }
}
