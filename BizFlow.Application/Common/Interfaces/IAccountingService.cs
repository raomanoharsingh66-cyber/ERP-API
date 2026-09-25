using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Accounting;
using BizFlow.Domain.Enums;

namespace BizFlow.Application.Common.Interfaces;

public interface IAccountingService
{
    // Chart of Accounts
    Task<IReadOnlyList<AccountDto>> GetAccountsAsync(AccountType? type = null, CancellationToken cancellationToken = default);
    Task<AccountDto?> GetAccountByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AccountDto> CreateAccountAsync(CreateAccountDto dto, CancellationToken cancellationToken = default);
    Task<AccountDto> UpdateAccountAsync(Guid id, UpdateAccountDto dto, CancellationToken cancellationToken = default);

    // Journal Entries / Vouchers
    Task<PagedResult<JournalEntryDto>> GetJournalEntriesAsync(JournalEntryFilterDto filter, CancellationToken cancellationToken = default);
    Task<JournalEntryDto?> GetJournalEntryByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<JournalEntryDto> CreateJournalEntryAsync(CreateJournalEntryDto dto, CancellationToken cancellationToken = default);

    // Financial Reports
    Task<TrialBalanceDto> GetTrialBalanceAsync(DateTime? asOfDate = null, CancellationToken cancellationToken = default);
    Task<ProfitLossReportDto> GetProfitLossAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<BalanceSheetReportDto> GetBalanceSheetAsync(DateTime? asOfDate = null, CancellationToken cancellationToken = default);
    Task<GstSummaryReportDto> GetGstSummaryAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
}
