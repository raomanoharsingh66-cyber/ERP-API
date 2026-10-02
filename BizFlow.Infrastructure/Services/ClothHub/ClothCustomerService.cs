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

public class ClothCustomerService : IClothCustomerService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ClothCustomerService(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<List<ClothCustomerDto>> GetCustomersAsync(string? search = null, CancellationToken cancellationToken = default)
    {
        await EnsureSeedCustomersAsync(cancellationToken);

        var query = _context.ClothCustomers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c =>
                c.CustomerName.ToLower().Contains(term) ||
                (c.Phone != null && c.Phone.Contains(term)) ||
                (c.City != null && c.City.ToLower().Contains(term)));
        }

        var list = await query
            .OrderByDescending(c => c.CurrentOutstanding)
            .ThenBy(c => c.CustomerName)
            .ToListAsync(cancellationToken);

        return list.Select(MapToDto).ToList();
    }

    public async Task<ClothCustomerDto?> GetCustomerByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ClothCustomers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return entity == null ? null : MapToDto(entity);
    }

    public async Task<ClothCustomerDto> CreateCustomerAsync(CreateClothCustomerDto dto, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.BusinessId ?? Guid.Empty;

        var customer = new ClothCustomer
        {
            BusinessId = businessId,
            CustomerName = dto.CustomerName.Trim(),
            Phone = dto.Phone?.Trim(),
            Email = dto.Email?.Trim(),
            Address = dto.Address?.Trim(),
            City = dto.City?.Trim(),
            Gstin = dto.Gstin?.Trim(),
            CreditLimit = dto.CreditLimit > 0 ? dto.CreditLimit : 5000,
            CurrentOutstanding = 0,
            TotalSpentAmount = 0,
            TotalVisitsCount = 0,
            DateOfBirth = dto.DateOfBirth,
            AnniversaryDate = dto.AnniversaryDate,
            Notes = dto.Notes?.Trim(),
            IsActive = true
        };

        _context.ClothCustomers.Add(customer);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(customer);
    }

    public async Task<ClothCustomerDto> SettleCreditAsync(SettleCustomerCreditDto dto, CancellationToken cancellationToken = default)
    {
        var customer = await _context.ClothCustomers
            .FirstOrDefaultAsync(c => c.Id == dto.CustomerId, cancellationToken);

        if (customer == null)
            throw new InvalidOperationException("Customer account not found");

        if (dto.PaymentAmount <= 0)
            throw new InvalidOperationException("Settlement payment amount must be greater than zero");

        customer.CurrentOutstanding = Math.Max(0, customer.CurrentOutstanding - dto.PaymentAmount);

        await _context.SaveChangesAsync(cancellationToken);
        return MapToDto(customer);
    }

    private async Task EnsureSeedCustomersAsync(CancellationToken cancellationToken)
    {
        var businessId = _currentUserService.BusinessId ?? Guid.Empty;
        var any = await _context.ClothCustomers.AnyAsync(cancellationToken);
        if (any) return;

        var seeds = new List<ClothCustomer>
        {
            new() { BusinessId = businessId, CustomerName = "Rahul Sharma", Phone = "9820011223", Email = "rahul.sharma@example.com", City = "Mumbai", CreditLimit = 10000, CurrentOutstanding = 3500, TotalSpentAmount = 24500, TotalVisitsCount = 8, IsActive = true, Notes = "VIP Customer - Prefers formal Raymond fabrics" },
            new() { BusinessId = businessId, CustomerName = "Priya Patel", Phone = "9819922334", Email = "priya.patel@example.com", City = "Thane", CreditLimit = 8000, CurrentOutstanding = 0, TotalSpentAmount = 18200, TotalVisitsCount = 5, IsActive = true, Notes = "Prefers Silk and Rayon festive Kurtis" },
            new() { BusinessId = businessId, CustomerName = "Amit Verma", Phone = "9833344556", Email = "amit.v@example.com", City = "Navi Mumbai", CreditLimit = 15000, CurrentOutstanding = 6800, TotalSpentAmount = 39000, TotalVisitsCount = 12, IsActive = true, Notes = "Corporate bulk buyer - Festive kurtas" },
            new() { BusinessId = businessId, CustomerName = "Pooja Malhotra", Phone = "9844455667", Email = "pooja.m@example.com", City = "Mumbai", CreditLimit = 5000, CurrentOutstanding = 1200, TotalSpentAmount = 12400, TotalVisitsCount = 4, IsActive = true, Notes = "Denim and casual wear buyer" }
        };

        _context.ClothCustomers.AddRange(seeds);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static ClothCustomerDto MapToDto(ClothCustomer entity)
    {
        return new ClothCustomerDto
        {
            Id = entity.Id,
            CustomerName = entity.CustomerName,
            Phone = entity.Phone,
            Email = entity.Email,
            Address = entity.Address,
            City = entity.City,
            Gstin = entity.Gstin,
            CreditLimit = entity.CreditLimit,
            CurrentOutstanding = entity.CurrentOutstanding,
            TotalSpentAmount = entity.TotalSpentAmount,
            TotalVisitsCount = entity.TotalVisitsCount,
            DateOfBirth = entity.DateOfBirth,
            AnniversaryDate = entity.AnniversaryDate,
            Notes = entity.Notes,
            IsActive = entity.IsActive
        };
    }
}
