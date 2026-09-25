using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Accounting;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BizFlow.Infrastructure.Services;

public class AccountingService : IAccountingService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<AccountingService> _logger;

    public AccountingService(
        IApplicationDbContext _context,
        ICurrentUserService _currentUserService,
        IDateTimeProvider _dateTimeProvider,
        ILogger<AccountingService> _logger)
    {
        this._context = _context;
        this._currentUserService = _currentUserService;
        this._dateTimeProvider = _dateTimeProvider;
        this._logger = _logger;
    }

    private Guid RequireBusinessId()
    {
        if (_currentUserService.BusinessId != null)
        {
            return _currentUserService.BusinessId.Value;
        }

        var defaultBusiness = _context.Businesses.OrderBy(b => b.CreatedOn).FirstOrDefault();
        if (defaultBusiness != null)
        {
            return defaultBusiness.Id;
        }

        throw new BusinessRuleException("A business context is required to execute accounting actions.");
    }

    #region Chart of Accounts

    public async Task<IReadOnlyList<AccountDto>> GetAccountsAsync(AccountType? type = null, CancellationToken cancellationToken = default)
    {
        var businessId = RequireBusinessId();
        var query = _context.Accounts
            .AsNoTracking()
            .Where(a => a.BusinessId == businessId);

        if (type.HasValue)
        {
            query = query.Where(a => a.Type == type.Value);
        }

        var accounts = await query
            .OrderBy(a => a.AccountCode)
            .ToListAsync(cancellationToken);

        return accounts.Select(MapToAccountDto).ToList();
    }

    public async Task<AccountDto?> GetAccountByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var businessId = RequireBusinessId();
        var account = await _context.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id && a.BusinessId == businessId, cancellationToken);

        return account == null ? null : MapToAccountDto(account);
    }

    public async Task<AccountDto> CreateAccountAsync(CreateAccountDto dto, CancellationToken cancellationToken = default)
    {
        var businessId = RequireBusinessId();

        var exists = await _context.Accounts
            .AnyAsync(a => a.BusinessId == businessId && a.AccountCode.ToLower() == dto.AccountCode.Trim().ToLower(), cancellationToken);

        if (exists)
        {
            throw new BusinessRuleException($"Account code '{dto.AccountCode}' is already in use.");
        }

        var account = new Account
        {
            BusinessId = businessId,
            AccountCode = dto.AccountCode.Trim().ToUpperInvariant(),
            AccountName = dto.AccountName.Trim(),
            Type = dto.Type,
            Subtype = dto.Subtype?.Trim(),
            Description = dto.Description?.Trim(),
            CurrentBalance = dto.InitialBalance,
            IsSystemAccount = false,
            IsActive = true
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created new account '{Code} - {Name}' for business {BusinessId}",
            account.AccountCode, account.AccountName, businessId);

        return MapToAccountDto(account);
    }

    public async Task<AccountDto> UpdateAccountAsync(Guid id, UpdateAccountDto dto, CancellationToken cancellationToken = default)
    {
        var businessId = RequireBusinessId();
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == id && a.BusinessId == businessId, cancellationToken);

        if (account == null)
        {
            throw new NotFoundException(nameof(Account), id);
        }

        account.AccountName = dto.AccountName.Trim();
        account.Subtype = dto.Subtype?.Trim();
        account.Description = dto.Description?.Trim();
        account.IsActive = dto.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        return MapToAccountDto(account);
    }

    #endregion

    #region Journal Entries

    public async Task<PagedResult<JournalEntryDto>> GetJournalEntriesAsync(JournalEntryFilterDto filter, CancellationToken cancellationToken = default)
    {
        var businessId = RequireBusinessId();
        var query = _context.JournalEntries
            .AsNoTracking()
            .Include(j => j.Lines)
                .ThenInclude(l => l.Account)
            .Where(j => j.BusinessId == businessId);

        if (filter.FromDate.HasValue)
        {
            query = query.Where(j => j.EntryDate >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(j => j.EntryDate <= filter.ToDate.Value);
        }

        if (filter.EntryType.HasValue)
        {
            query = query.Where(j => j.EntryType == filter.EntryType.Value);
        }

        if (filter.AccountId.HasValue)
        {
            query = query.Where(j => j.Lines.Any(l => l.AccountId == filter.AccountId.Value));
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(j =>
                j.EntryNumber.ToLower().Contains(search) ||
                (j.Reference != null && j.Reference.ToLower().Contains(search)) ||
                j.Narration.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var entries = await query
            .OrderByDescending(j => j.EntryDate)
            .ThenByDescending(j => j.CreatedOn)
            .Skip((filter.PageIndex - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        var items = entries.Select(MapToJournalEntryDto).ToList();

        return new PagedResult<JournalEntryDto>(items, totalCount, filter.PageIndex, filter.PageSize);
    }

    public async Task<JournalEntryDto?> GetJournalEntryByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var businessId = RequireBusinessId();
        var entry = await _context.JournalEntries
            .AsNoTracking()
            .Include(j => j.Lines)
                .ThenInclude(l => l.Account)
            .FirstOrDefaultAsync(j => j.Id == id && j.BusinessId == businessId, cancellationToken);

        return entry == null ? null : MapToJournalEntryDto(entry);
    }

    public async Task<JournalEntryDto> CreateJournalEntryAsync(CreateJournalEntryDto dto, CancellationToken cancellationToken = default)
    {
        var businessId = RequireBusinessId();

        if (dto.Lines == null || dto.Lines.Count < 2)
        {
            throw new BusinessRuleException("A double-entry journal voucher must contain at least 2 line items.");
        }

        var totalDebit = dto.Lines.Sum(l => l.Debit);
        var totalCredit = dto.Lines.Sum(l => l.Credit);

        if (Math.Abs(totalDebit - totalCredit) >= 0.01m || totalDebit <= 0)
        {
            throw new BusinessRuleException(
                $"Strict Double-Entry Rule Violation: Total Debits (₹{totalDebit:N2}) must equal Total Credits (₹{totalCredit:N2}) and be greater than zero.");
        }

        // Validate all referenced accounts belong to this business and are active
        var accountIds = dto.Lines.Select(l => l.AccountId).Distinct().ToList();
        var accounts = await _context.Accounts
            .Where(a => accountIds.Contains(a.Id) && a.BusinessId == businessId && a.IsActive)
            .ToDictionaryAsync(a => a.Id, cancellationToken);

        if (accounts.Count != accountIds.Count)
        {
            throw new BusinessRuleException("One or more referenced accounts were not found or are deactivated.");
        }

        // Auto-generate Entry Number (JE-YYYY-NNNN)
        var year = _dateTimeProvider.UtcNow.Year;
        var prefix = $"JE-{year}-";
        var lastEntry = await _context.JournalEntries
            .IgnoreQueryFilters()
            .Where(j => j.BusinessId == businessId && j.EntryNumber.StartsWith(prefix))
            .OrderByDescending(j => j.EntryNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var nextNumber = 1;
        if (lastEntry != null && int.TryParse(lastEntry.EntryNumber.Replace(prefix, ""), out var num))
        {
            nextNumber = num + 1;
        }
        var entryNumber = $"{prefix}{nextNumber:D4}";

        var entry = new JournalEntry
        {
            BusinessId = businessId,
            EntryNumber = entryNumber,
            EntryDate = dto.EntryDate,
            Reference = dto.Reference?.Trim(),
            Narration = dto.Narration.Trim(),
            EntryType = dto.EntryType,
            TotalDebit = totalDebit,
            TotalCredit = totalCredit,
            IsPosted = true
        };

        foreach (var lineDto in dto.Lines)
        {
            var line = new JournalEntryLine
            {
                AccountId = lineDto.AccountId,
                Debit = lineDto.Debit,
                Credit = lineDto.Credit,
                Description = lineDto.Description?.Trim()
            };
            entry.Lines.Add(line);

            // Update Account Current Balance according to standard accounting nature
            var account = accounts[lineDto.AccountId];
            if (account.Type is AccountType.Asset or AccountType.Expense)
            {
                // Normal balance is Debit: Debit increases, Credit decreases
                account.CurrentBalance += (lineDto.Debit - lineDto.Credit);
            }
            else
            {
                // Normal balance is Credit (Liability, Equity, Revenue): Credit increases, Debit decreases
                account.CurrentBalance += (lineDto.Credit - lineDto.Debit);
            }
        }

        _context.JournalEntries.Add(entry);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Posted journal entry {EntryNumber} with balance ₹{Amount:N2} for business {BusinessId}",
            entry.EntryNumber, totalDebit, businessId);

        // Reload to map with account references
        return (await GetJournalEntryByIdAsync(entry.Id, cancellationToken))!;
    }

    #endregion

    #region Financial Reports

    public async Task<TrialBalanceDto> GetTrialBalanceAsync(DateTime? asOfDate = null, CancellationToken cancellationToken = default)
    {
        var businessId = RequireBusinessId();
        var targetDate = asOfDate ?? _dateTimeProvider.UtcDateTime;

        var accounts = await _context.Accounts
            .AsNoTracking()
            .Where(a => a.BusinessId == businessId && a.IsActive)
            .OrderBy(a => a.AccountCode)
            .ToListAsync(cancellationToken);

        var items = new List<TrialBalanceItemDto>();
        decimal totalDebit = 0;
        decimal totalCredit = 0;

        foreach (var account in accounts)
        {
            decimal debit = 0;
            decimal credit = 0;

            if (account.Type is AccountType.Asset or AccountType.Expense)
            {
                if (account.CurrentBalance >= 0)
                {
                    debit = account.CurrentBalance;
                }
                else
                {
                    credit = Math.Abs(account.CurrentBalance);
                }
            }
            else
            {
                if (account.CurrentBalance >= 0)
                {
                    credit = account.CurrentBalance;
                }
                else
                {
                    debit = Math.Abs(account.CurrentBalance);
                }
            }

            items.Add(new TrialBalanceItemDto
            {
                AccountId = account.Id,
                AccountCode = account.AccountCode,
                AccountName = account.AccountName,
                Type = account.Type,
                Debit = debit,
                Credit = credit
            });

            totalDebit += debit;
            totalCredit += credit;
        }

        return new TrialBalanceDto
        {
            AsOfDate = targetDate,
            Items = items,
            TotalDebit = totalDebit,
            TotalCredit = totalCredit
        };
    }

    public async Task<ProfitLossReportDto> GetProfitLossAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var businessId = RequireBusinessId();
        var now = _dateTimeProvider.UtcDateTime;
        var from = fromDate ?? new DateTime(now.Year, 1, 1);
        var to = toDate ?? now;

        var accounts = await _context.Accounts
            .AsNoTracking()
            .Where(a => a.BusinessId == businessId && a.IsActive &&
                       (a.Type == AccountType.Revenue || a.Type == AccountType.Expense))
            .OrderBy(a => a.AccountCode)
            .ToListAsync(cancellationToken);

        var revenueCategory = new ProfitLossCategoryDto { CategoryName = "Operating Revenue" };
        var cogsCategory = new ProfitLossCategoryDto { CategoryName = "Cost of Goods Sold" };
        var opexCategory = new ProfitLossCategoryDto { CategoryName = "Operating Expenses" };

        foreach (var account in accounts)
        {
            if (account.Type == AccountType.Revenue)
            {
                revenueCategory.Items.Add(new ProfitLossItemDto
                {
                    AccountCode = account.AccountCode,
                    AccountName = account.AccountName,
                    Amount = account.CurrentBalance
                });
            }
            else if (account.Type == AccountType.Expense)
            {
                // Check if COGS (code starting with 5 or subtype containing COGS)
                if (account.AccountCode.StartsWith("5") || (account.Subtype != null && account.Subtype.Contains("COGS", StringComparison.OrdinalIgnoreCase)))
                {
                    cogsCategory.Items.Add(new ProfitLossItemDto
                    {
                        AccountCode = account.AccountCode,
                        AccountName = account.AccountName,
                        Amount = account.CurrentBalance
                    });
                }
                else
                {
                    opexCategory.Items.Add(new ProfitLossItemDto
                    {
                        AccountCode = account.AccountCode,
                        AccountName = account.AccountName,
                        Amount = account.CurrentBalance
                    });
                }
            }
        }

        revenueCategory.Subtotal = revenueCategory.Items.Sum(i => i.Amount);
        cogsCategory.Subtotal = cogsCategory.Items.Sum(i => i.Amount);
        opexCategory.Subtotal = opexCategory.Items.Sum(i => i.Amount);

        var grossProfit = revenueCategory.Subtotal - cogsCategory.Subtotal;
        var netProfit = grossProfit - opexCategory.Subtotal;
        var margin = revenueCategory.Subtotal > 0 ? (netProfit / revenueCategory.Subtotal) * 100 : 0;

        return new ProfitLossReportDto
        {
            FromDate = from,
            ToDate = to,
            Revenue = revenueCategory,
            CostOfGoodsSold = cogsCategory,
            GrossProfit = grossProfit,
            OperatingExpenses = opexCategory,
            TotalOperatingExpenses = opexCategory.Subtotal,
            NetProfit = netProfit,
            NetProfitMarginPercentage = Math.Round(margin, 2)
        };
    }

    public async Task<BalanceSheetReportDto> GetBalanceSheetAsync(DateTime? asOfDate = null, CancellationToken cancellationToken = default)
    {
        var businessId = RequireBusinessId();
        var targetDate = asOfDate ?? _dateTimeProvider.UtcDateTime;

        var accounts = await _context.Accounts
            .AsNoTracking()
            .Where(a => a.BusinessId == businessId && a.IsActive &&
                       (a.Type == AccountType.Asset || a.Type == AccountType.Liability || a.Type == AccountType.Equity))
            .OrderBy(a => a.AccountCode)
            .ToListAsync(cancellationToken);

        var currentAssets = new BalanceSheetCategoryDto { CategoryName = "Current Assets" };
        var nonCurrentAssets = new BalanceSheetCategoryDto { CategoryName = "Non-Current Assets" };

        var currentLiabilities = new BalanceSheetCategoryDto { CategoryName = "Current Liabilities" };
        var nonCurrentLiabilities = new BalanceSheetCategoryDto { CategoryName = "Non-Current Liabilities" };

        var equityCategory = new BalanceSheetCategoryDto { CategoryName = "Capital & Equity" };

        foreach (var account in accounts)
        {
            var item = new BalanceSheetItemDto
            {
                AccountCode = account.AccountCode,
                AccountName = account.AccountName,
                Balance = account.CurrentBalance
            };

            switch (account.Type)
            {
                case AccountType.Asset:
                    if (account.Subtype != null && account.Subtype.Contains("Fixed", StringComparison.OrdinalIgnoreCase))
                    {
                        nonCurrentAssets.Items.Add(item);
                    }
                    else
                    {
                        currentAssets.Items.Add(item);
                    }
                    break;

                case AccountType.Liability:
                    if (account.Subtype != null && account.Subtype.Contains("Long Term", StringComparison.OrdinalIgnoreCase))
                    {
                        nonCurrentLiabilities.Items.Add(item);
                    }
                    else
                    {
                        currentLiabilities.Items.Add(item);
                    }
                    break;

                case AccountType.Equity:
                    equityCategory.Items.Add(item);
                    break;
            }
        }

        currentAssets.Subtotal = currentAssets.Items.Sum(i => i.Balance);
        nonCurrentAssets.Subtotal = nonCurrentAssets.Items.Sum(i => i.Balance);
        var totalAssets = currentAssets.Subtotal + nonCurrentAssets.Subtotal;

        currentLiabilities.Subtotal = currentLiabilities.Items.Sum(i => i.Balance);
        nonCurrentLiabilities.Subtotal = nonCurrentLiabilities.Items.Sum(i => i.Balance);
        var totalLiabilities = currentLiabilities.Subtotal + nonCurrentLiabilities.Subtotal;

        equityCategory.Subtotal = equityCategory.Items.Sum(i => i.Balance);

        // Calculate Net Profit YTD dynamically for Retained Earnings roll-up
        var pnl = await GetProfitLossAsync(null, targetDate, cancellationToken);
        var retainedEarnings = pnl.NetProfit;
        var totalEquity = equityCategory.Subtotal + retainedEarnings;

        var totalLiabilitiesAndEquity = totalLiabilities + totalEquity;

        return new BalanceSheetReportDto
        {
            AsOfDate = targetDate,
            CurrentAssets = currentAssets,
            NonCurrentAssets = nonCurrentAssets,
            TotalAssets = totalAssets,
            CurrentLiabilities = currentLiabilities,
            NonCurrentLiabilities = nonCurrentLiabilities,
            TotalLiabilities = totalLiabilities,
            Equity = equityCategory,
            RetainedEarnings = retainedEarnings,
            TotalEquity = totalEquity,
            TotalLiabilitiesAndEquity = totalLiabilitiesAndEquity
        };
    }

    public async Task<GstSummaryReportDto> GetGstSummaryAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var businessId = RequireBusinessId();
        var now = _dateTimeProvider.UtcDateTime;
        var from = fromDate ?? new DateTime(now.Year, now.Month, 1);
        var to = toDate ?? now;

        // 1. Outward (Sales) Tax Liabilities
        var salesInvoices = await _context.SalesInvoices
            .AsNoTracking()
            .Where(inv => inv.BusinessId == businessId &&
                          inv.InvoiceDate >= from && inv.InvoiceDate <= to &&
                          inv.Status != InvoiceStatus.Cancelled && inv.Status != InvoiceStatus.Draft)
            .ToListAsync(cancellationToken);

        var outwardTaxable = salesInvoices.Sum(i => i.SubTotal);
        var cgstOutput = salesInvoices.Sum(i => i.CgstAmount);
        var sgstOutput = salesInvoices.Sum(i => i.SgstAmount);
        var igstOutput = salesInvoices.Sum(i => i.IgstAmount);
        var totalOutput = cgstOutput + sgstOutput + igstOutput;

        // 2. Inward (Purchases) Input Tax Credit (ITC)
        var purchaseBills = await _context.PurchaseBills
            .AsNoTracking()
            .Where(b => b.BusinessId == businessId &&
                        b.BillDate >= from && b.BillDate <= to &&
                        b.Status != BillStatus.Cancelled && b.Status != BillStatus.Draft)
            .ToListAsync(cancellationToken);

        var inwardTaxable = purchaseBills.Sum(b => b.SubTotal);
        var cgstInput = purchaseBills.Sum(b => b.CgstAmount);
        var sgstInput = purchaseBills.Sum(b => b.SgstAmount);
        var igstInput = purchaseBills.Sum(b => b.IgstAmount);
        var totalInput = cgstInput + sgstInput + igstInput;

        // 3. Net Tax Payable (Output - Input)
        var netCgst = Math.Max(0, cgstOutput - cgstInput);
        var netSgst = Math.Max(0, sgstOutput - sgstInput);
        var netIgst = Math.Max(0, igstOutput - igstInput);
        var netTotal = netCgst + netSgst + netIgst;

        return new GstSummaryReportDto
        {
            FromDate = from,
            ToDate = to,
            OutwardTaxableAmount = outwardTaxable,
            CgstOutput = cgstOutput,
            SgstOutput = sgstOutput,
            IgstOutput = igstOutput,
            TotalOutputTax = totalOutput,
            InwardTaxableAmount = inwardTaxable,
            CgstInputCredit = cgstInput,
            SgstInputCredit = sgstInput,
            IgstInputCredit = igstInput,
            TotalInputCredit = totalInput,
            NetCgstPayable = netCgst,
            NetSgstPayable = netSgst,
            NetIgstPayable = netIgst,
            NetTotalTaxPayable = netTotal
        };
    }

    #endregion

    #region Mappers

    private static AccountDto MapToAccountDto(Account a) => new()
    {
        Id = a.Id,
        AccountCode = a.AccountCode,
        AccountName = a.AccountName,
        Type = a.Type,
        Subtype = a.Subtype,
        Description = a.Description,
        CurrentBalance = a.CurrentBalance,
        IsSystemAccount = a.IsSystemAccount,
        IsActive = a.IsActive,
        CreatedAt = a.CreatedOn.UtcDateTime
    };

    private static JournalEntryDto MapToJournalEntryDto(JournalEntry j) => new()
    {
        Id = j.Id,
        EntryNumber = j.EntryNumber,
        EntryDate = j.EntryDate,
        Reference = j.Reference,
        Narration = j.Narration,
        EntryType = j.EntryType,
        TotalDebit = j.TotalDebit,
        TotalCredit = j.TotalCredit,
        IsPosted = j.IsPosted,
        CreatedAt = j.CreatedOn.UtcDateTime,
        Lines = j.Lines.Select(l => new JournalEntryLineDto
        {
            Id = l.Id,
            AccountId = l.AccountId,
            AccountCode = l.Account?.AccountCode ?? string.Empty,
            AccountName = l.Account?.AccountName ?? string.Empty,
            Debit = l.Debit,
            Credit = l.Credit,
            Description = l.Description
        }).ToList()
    };

    #endregion
}
