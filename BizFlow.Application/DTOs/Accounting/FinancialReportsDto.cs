using BizFlow.Domain.Enums;

namespace BizFlow.Application.DTOs.Accounting;

public class TrialBalanceItemDto
{
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public string TypeName => Type.ToString();
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
}

public class TrialBalanceDto
{
    public DateTime AsOfDate { get; set; }
    public List<TrialBalanceItemDto> Items { get; set; } = new();
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public bool IsBalanced => Math.Abs(TotalDebit - TotalCredit) < 0.01m;
}

public class ProfitLossItemDto
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class ProfitLossCategoryDto
{
    public string CategoryName { get; set; } = string.Empty;
    public List<ProfitLossItemDto> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
}

public class ProfitLossReportDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public ProfitLossCategoryDto Revenue { get; set; } = new();
    public ProfitLossCategoryDto CostOfGoodsSold { get; set; } = new();
    public decimal GrossProfit { get; set; }
    public ProfitLossCategoryDto OperatingExpenses { get; set; } = new();
    public decimal TotalOperatingExpenses { get; set; }
    public decimal NetProfit { get; set; }
    public decimal NetProfitMarginPercentage { get; set; }
}

public class BalanceSheetItemDto
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}

public class BalanceSheetCategoryDto
{
    public string CategoryName { get; set; } = string.Empty;
    public List<BalanceSheetItemDto> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
}

public class BalanceSheetReportDto
{
    public DateTime AsOfDate { get; set; }
    public BalanceSheetCategoryDto CurrentAssets { get; set; } = new();
    public BalanceSheetCategoryDto NonCurrentAssets { get; set; } = new();
    public decimal TotalAssets { get; set; }

    public BalanceSheetCategoryDto CurrentLiabilities { get; set; } = new();
    public BalanceSheetCategoryDto NonCurrentLiabilities { get; set; } = new();
    public decimal TotalLiabilities { get; set; }

    public BalanceSheetCategoryDto Equity { get; set; } = new();
    public decimal RetainedEarnings { get; set; }
    public decimal TotalEquity { get; set; }

    public decimal TotalLiabilitiesAndEquity { get; set; }
    public bool IsBalanced => Math.Abs(TotalAssets - TotalLiabilitiesAndEquity) < 0.01m;
}

public class GstSummaryReportDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }

    // Outward (Sales) GST Liability
    public decimal OutwardTaxableAmount { get; set; }
    public decimal CgstOutput { get; set; }
    public decimal SgstOutput { get; set; }
    public decimal IgstOutput { get; set; }
    public decimal TotalOutputTax { get; set; }

    // Inward (Purchases) Input Tax Credit (ITC)
    public decimal InwardTaxableAmount { get; set; }
    public decimal CgstInputCredit { get; set; }
    public decimal SgstInputCredit { get; set; }
    public decimal IgstInputCredit { get; set; }
    public decimal TotalInputCredit { get; set; }

    // Net GST Payable / (Credit Carried Forward)
    public decimal NetCgstPayable { get; set; }
    public decimal NetSgstPayable { get; set; }
    public decimal NetIgstPayable { get; set; }
    public decimal NetTotalTaxPayable { get; set; }
}
