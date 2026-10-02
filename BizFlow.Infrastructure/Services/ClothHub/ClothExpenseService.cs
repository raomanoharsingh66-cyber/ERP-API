using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.DTOs.ClothHub;
using BizFlow.Domain.Entities.ClothHub;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Services.ClothHub;

public class ClothExpenseService : IClothExpenseService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ClothExpenseService(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<List<ClothExpenseDto>> GetExpensesAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureSeedExpensesAsync(cancellationToken);

        var query = _context.ClothExpenses.AsNoTracking().AsQueryable();

        if (fromDate.HasValue)
        {
            var f = fromDate.Value.Date;
            query = query.Where(e => e.ExpenseDate >= f);
        }

        if (toDate.HasValue)
        {
            var t = toDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(e => e.ExpenseDate <= t);
        }

        if (!string.IsNullOrWhiteSpace(category) && category != "All")
        {
            query = query.Where(e => e.ExpenseCategory == category);
        }

        var list = await query
            .OrderByDescending(e => e.ExpenseDate)
            .ToListAsync(cancellationToken);

        return list.Select(MapToDto).ToList();
    }

    public async Task<ClothExpenseDto> CreateExpenseAsync(CreateClothExpenseDto dto, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.BusinessId ?? Guid.Empty;
        var today = dto.ExpenseDate ?? DateTime.UtcNow;

        var prefix = $"EXP-{today:yyyyMMdd}";
        var count = await _context.ClothExpenses
            .IgnoreQueryFilters()
            .CountAsync(e => e.VoucherNumber.StartsWith(prefix), cancellationToken);

        var voucherNumber = $"{prefix}-{(count + 1):D4}";

        var expense = new ClothExpense
        {
            BusinessId = businessId,
            VoucherNumber = voucherNumber,
            ExpenseDate = today,
            ExpenseCategory = dto.ExpenseCategory,
            Amount = dto.Amount,
            PaymentMode = dto.PaymentMode,
            PaidTo = dto.PaidTo?.Trim(),
            Notes = dto.Notes?.Trim(),
            ApprovedBy = _currentUserService.Email ?? "Manager"
        };

        _context.ClothExpenses.Add(expense);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(expense);
    }

    public async Task<ClothExpenseSummaryDto> GetExpenseSummaryAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSeedExpensesAsync(cancellationToken);

        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var all = await _context.ClothExpenses.AsNoTracking().ToListAsync(cancellationToken);

        var todaySum = all.Where(e => e.ExpenseDate.Date == today).Sum(e => e.Amount);
        var monthSum = all.Where(e => e.ExpenseDate >= monthStart).Sum(e => e.Amount);
        var totalSum = all.Sum(e => e.Amount);

        var categoryBreakdown = all
            .GroupBy(e => e.ExpenseCategory)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

        return new ClothExpenseSummaryDto
        {
            TodayExpenses = todaySum,
            MonthExpenses = monthSum,
            TotalExpenses = totalSum,
            TotalExpensesCount = all.Count,
            CategoryBreakdown = categoryBreakdown
        };
    }

    private async Task EnsureSeedExpensesAsync(CancellationToken cancellationToken)
    {
        var businessId = _currentUserService.BusinessId ?? Guid.Empty;
        var any = await _context.ClothExpenses.AnyAsync(cancellationToken);
        if (any) return;

        var today = DateTime.UtcNow;

        var seeds = new List<ClothExpense>
        {
            new() { BusinessId = businessId, VoucherNumber = $"EXP-{today:yyyyMMdd}-0001", ExpenseDate = today.AddDays(-1), ExpenseCategory = "Staff Welfare / Tea & Snacks", Amount = 450, PaymentMode = "Cash", PaidTo = "Sharma Tea Stall", Notes = "Daily tea and snacks for sales staff" },
            new() { BusinessId = businessId, VoucherNumber = $"EXP-{today:yyyyMMdd}-0002", ExpenseDate = today.AddDays(-2), ExpenseCategory = "Packaging, Bags & Tags", Amount = 4800, PaymentMode = "UPI", PaidTo = "Metro Paper Print Bags", Notes = "500 pcs Cloth Hub branded non-woven carry bags" },
            new() { BusinessId = businessId, VoucherNumber = $"EXP-{today:yyyyMMdd}-0003", ExpenseDate = today.AddDays(-3), ExpenseCategory = "Tailoring & Alterations", Amount = 1250, PaymentMode = "Cash", PaidTo = "Master Tailor Aslam", Notes = "Trouser length alterations for 10 walk-in customer purchases" },
            new() { BusinessId = businessId, VoucherNumber = $"EXP-{today:yyyyMMdd}-0004", ExpenseDate = today.AddDays(-5), ExpenseCategory = "Shop Utility / Power", Amount = 7200, PaymentMode = "Bank Transfer", PaidTo = "Electricity Distribution Co", Notes = "Showroom Air Conditioning and Display Lighting bill" }
        };

        _context.ClothExpenses.AddRange(seeds);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static ClothExpenseDto MapToDto(ClothExpense entity)
    {
        return new ClothExpenseDto
        {
            Id = entity.Id,
            VoucherNumber = entity.VoucherNumber,
            ExpenseDate = entity.ExpenseDate,
            ExpenseCategory = entity.ExpenseCategory,
            Amount = entity.Amount,
            PaymentMode = entity.PaymentMode,
            PaidTo = entity.PaidTo,
            Notes = entity.Notes,
            ApprovedBy = entity.ApprovedBy
        };
    }
}
