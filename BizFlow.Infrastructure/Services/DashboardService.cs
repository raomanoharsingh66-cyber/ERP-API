using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Dashboard;
using BizFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BizFlow.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        ILogger<DashboardService> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
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

        throw new BusinessRuleException("A business context is required to query dashboard metrics.");
    }

    public async Task<DashboardMetricsDto> GetExecutiveDashboardAsync(CancellationToken cancellationToken = default)
    {
        var businessId = RequireBusinessId();
        var now = _dateTimeProvider.UtcNow;

        // 1. Executive Financial KPIs
        var invoices = await _context.SalesInvoices
            .AsNoTracking()
            .Where(i => i.BusinessId == businessId && i.Status != InvoiceStatus.Cancelled && i.Status != InvoiceStatus.Draft)
            .ToListAsync(cancellationToken);

        var totalRevenue = invoices.Sum(i => i.TotalAmount);
        var totalReceivables = invoices.Sum(i => i.BalanceAmount);

        var bills = await _context.PurchaseBills
            .AsNoTracking()
            .Where(b => b.BusinessId == businessId && b.Status != BillStatus.Cancelled && b.Status != BillStatus.Draft)
            .ToListAsync(cancellationToken);

        var totalPayables = bills.Sum(b => b.BalanceAmount);

        // Inventory Valuation & Health
        var stocks = await _context.InventoryStocks
            .AsNoTracking()
            .Include(s => s.Product)
            .Include(s => s.Warehouse)
            .Where(s => s.BusinessId == businessId)
            .ToListAsync(cancellationToken);

        var totalProducts = await _context.Products
            .AsNoTracking()
            .CountAsync(p => p.BusinessId == businessId && p.IsActive, cancellationToken);

        var inventoryValuation = stocks.Sum(s => s.QuantityOnHand * (s.Product?.PurchasePrice ?? 0m));
        var totalStockQty = stocks.Sum(s => s.QuantityOnHand);

        var lowStockAlerts = new List<LowStockAlertItem>();
        int lowStockCount = 0;
        int outOfStockCount = 0;

        foreach (var stock in stocks)
        {
            var reorder = stock.Product?.MinStockLevel ?? 0m;
            if (stock.QuantityOnHand <= 0)
            {
                outOfStockCount++;
                lowStockAlerts.Add(new LowStockAlertItem
                {
                    ProductId = stock.ProductId,
                    ProductName = stock.Product?.Name ?? "Unknown Product",
                    SKU = stock.Product?.SKU ?? string.Empty,
                    QuantityOnHand = stock.QuantityOnHand,
                    ReorderLevel = reorder,
                    WarehouseName = stock.Warehouse?.Name ?? "Warehouse"
                });
            }
            else if (stock.QuantityOnHand <= reorder)
            {
                lowStockCount++;
                lowStockAlerts.Add(new LowStockAlertItem
                {
                    ProductId = stock.ProductId,
                    ProductName = stock.Product?.Name ?? "Unknown Product",
                    SKU = stock.Product?.SKU ?? string.Empty,
                    QuantityOnHand = stock.QuantityOnHand,
                    ReorderLevel = reorder,
                    WarehouseName = stock.Warehouse?.Name ?? "Warehouse"
                });
            }
        }

        // Cash & Bank Liquidity + Net Profit YTD from General Ledger
        var accounts = await _context.Accounts
            .AsNoTracking()
            .Where(a => a.BusinessId == businessId && a.IsActive)
            .ToListAsync(cancellationToken);

        var cashBankBalance = accounts
            .Where(a => a.Type == AccountType.Asset &&
                       (a.AccountCode.StartsWith("10") ||
                        (a.Subtype != null && (a.Subtype.Contains("Bank", StringComparison.OrdinalIgnoreCase) || a.Subtype.Contains("Cash", StringComparison.OrdinalIgnoreCase)))))
            .Sum(a => a.CurrentBalance);

        var totalRevenueAccounts = accounts.Where(a => a.Type == AccountType.Revenue).Sum(a => a.CurrentBalance);
        var totalExpenseAccounts = accounts.Where(a => a.Type == AccountType.Expense).Sum(a => a.CurrentBalance);
        var netProfitYtd = totalRevenueAccounts - totalExpenseAccounts;

        // 2. Multi-Month Trends (Past 6 Months)
        var monthlyTrends = new List<MonthlyTrendItem>();
        for (int i = 5; i >= 0; i--)
        {
            var targetMonthDate = now.AddMonths(-i);
            var startOfMonth = new DateTime(targetMonthDate.Year, targetMonthDate.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddTicks(-1);

            var monthSales = invoices
                .Where(inv => inv.InvoiceDate >= startOfMonth && inv.InvoiceDate <= endOfMonth)
                .Sum(inv => inv.TotalAmount);

            var monthPurchases = bills
                .Where(b => b.BillDate >= startOfMonth && b.BillDate <= endOfMonth)
                .Sum(b => b.TotalAmount);

            monthlyTrends.Add(new MonthlyTrendItem
            {
                MonthName = targetMonthDate.ToString("MMM"),
                Year = targetMonthDate.Year,
                SalesRevenue = monthSales,
                PurchaseExpense = monthPurchases
            });
        }

        // 3. Cross-Module Activity Feed
        var activities = new List<RecentActivityItem>();

        // Recent Invoices
        var recentInvoices = await _context.SalesInvoices
            .AsNoTracking()
            .Include(i => i.Customer)
            .Where(i => i.BusinessId == businessId)
            .OrderByDescending(i => i.InvoiceDate)
            .Take(4)
            .ToListAsync(cancellationToken);

        foreach (var inv in recentInvoices)
        {
            activities.Add(new RecentActivityItem
            {
                ActivityType = "Invoice",
                Title = $"Invoice {inv.InvoiceNumber}",
                Subtitle = inv.Customer?.Name ?? "Customer",
                Amount = inv.TotalAmount,
                Timestamp = inv.InvoiceDate,
                StatusBadge = inv.Status.ToString(),
                ReferenceId = inv.Id.ToString()
            });
        }

        // Recent Vendor Bills
        var recentBills = await _context.PurchaseBills
            .AsNoTracking()
            .Include(b => b.Supplier)
            .Where(b => b.BusinessId == businessId)
            .OrderByDescending(b => b.BillDate)
            .Take(4)
            .ToListAsync(cancellationToken);

        foreach (var bill in recentBills)
        {
            activities.Add(new RecentActivityItem
            {
                ActivityType = "Bill",
                Title = $"Vendor Bill {bill.BillNumber}",
                Subtitle = bill.Supplier?.Name ?? "Supplier",
                Amount = bill.TotalAmount,
                Timestamp = bill.BillDate,
                StatusBadge = bill.Status.ToString(),
                ReferenceId = bill.Id.ToString()
            });
        }

        // Recent Journal Vouchers
        var recentJournals = await _context.JournalEntries
            .AsNoTracking()
            .Where(j => j.BusinessId == businessId)
            .OrderByDescending(j => j.EntryDate)
            .Take(4)
            .ToListAsync(cancellationToken);

        foreach (var j in recentJournals)
        {
            activities.Add(new RecentActivityItem
            {
                ActivityType = "Journal",
                Title = $"Journal {j.EntryNumber}",
                Subtitle = j.Narration,
                Amount = j.TotalDebit,
                Timestamp = j.EntryDate,
                StatusBadge = "Posted",
                ReferenceId = j.Id.ToString()
            });
        }

        var sortedActivities = activities
            .OrderByDescending(a => a.Timestamp)
            .Take(8)
            .ToList();

        return new DashboardMetricsDto
        {
            TotalRevenue = totalRevenue,
            TotalReceivables = totalReceivables,
            TotalPayables = totalPayables,
            InventoryValuation = inventoryValuation,
            CashBankBalance = cashBankBalance,
            NetProfitYTD = netProfitYtd,
            TotalProducts = totalProducts,
            TotalStockQuantity = totalStockQty,
            LowStockCount = lowStockCount,
            OutOfStockCount = outOfStockCount,
            MonthlyTrends = monthlyTrends,
            RecentActivities = sortedActivities,
            LowStockAlerts = lowStockAlerts.Take(6).ToList()
        };
    }

    public async Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(
        string? search = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var businessId = RequireBusinessId();
        var query = _context.AuditLogs
            .AsNoTracking()
            .Where(a => a.BusinessId == businessId);

        if (fromDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= toDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(a =>
                a.Action.ToLower().Contains(s) ||
                a.Entity.ToLower().Contains(s) ||
                a.EntityId.ToLower().Contains(s) ||
                (a.IpAddress != null && a.IpAddress.ToLower().Contains(s)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var logs = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Fetch user emails for display
        var userIds = logs.Where(l => l.UserId.HasValue).Select(l => l.UserId!.Value).Distinct().ToList();
        var users = await _context.Users
            .IgnoreQueryFilters()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email, cancellationToken);

        var items = logs.Select(l => new AuditLogDto
        {
            Id = l.Id,
            UserId = l.UserId,
            UserEmail = l.UserId.HasValue && users.TryGetValue(l.UserId.Value, out var email) ? email : "System Admin",
            Action = l.Action,
            Entity = l.Entity,
            EntityId = l.EntityId,
            OldValue = l.OldValue,
            NewValue = l.NewValue,
            IpAddress = l.IpAddress,
            Timestamp = l.Timestamp
        }).ToList();

        return new PagedResult<AuditLogDto>(items, totalCount, page, pageSize);
    }
}
