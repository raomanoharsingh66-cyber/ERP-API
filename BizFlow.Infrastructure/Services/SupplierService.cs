using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Purchases;
using BizFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Services;

public class SupplierService : ISupplierService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SupplierService(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApiResponse<PagedResult<SupplierDto>>> GetSuppliersAsync(
        string? search = null,
        bool? isActive = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Suppliers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(v =>
                v.Name.ToLower().Contains(s) ||
                v.SupplierCode.ToLower().Contains(s) ||
                (v.ContactPerson != null && v.ContactPerson.ToLower().Contains(s)) ||
                (v.Email != null && v.Email.ToLower().Contains(s)) ||
                (v.Phone != null && v.Phone.Contains(s)) ||
                (v.GSTIN != null && v.GSTIN.ToLower().Contains(s)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(v => v.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(v => v.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new SupplierDto
            {
                Id = v.Id,
                SupplierCode = v.SupplierCode,
                Name = v.Name,
                ContactPerson = v.ContactPerson,
                Email = v.Email,
                Phone = v.Phone,
                GSTIN = v.GSTIN,
                PAN = v.PAN,
                BillingAddress = v.BillingAddress,
                City = v.City,
                State = v.State,
                PostalCode = v.PostalCode,
                Country = v.Country,
                PaymentTermsDays = v.PaymentTermsDays,
                OutstandingPayable = v.OutstandingPayable,
                IsActive = v.IsActive,
                Notes = v.Notes,
                CreatedOn = v.CreatedOn
            })
            .ToListAsync(cancellationToken);

        var result = PagedResult<SupplierDto>.Create(items, totalCount, pageNumber, pageSize);
        return ApiResponse<PagedResult<SupplierDto>>.SuccessResult(result);
    }

    public async Task<ApiResponse<SupplierDto>> GetSupplierByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var v = await _context.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (v == null)
        {
            throw new NotFoundException("Supplier", id);
        }

        var dto = new SupplierDto
        {
            Id = v.Id,
            SupplierCode = v.SupplierCode,
            Name = v.Name,
            ContactPerson = v.ContactPerson,
            Email = v.Email,
            Phone = v.Phone,
            GSTIN = v.GSTIN,
            PAN = v.PAN,
            BillingAddress = v.BillingAddress,
            City = v.City,
            State = v.State,
            PostalCode = v.PostalCode,
            Country = v.Country,
            PaymentTermsDays = v.PaymentTermsDays,
            OutstandingPayable = v.OutstandingPayable,
            IsActive = v.IsActive,
            Notes = v.Notes,
            CreatedOn = v.CreatedOn
        };

        return ApiResponse<SupplierDto>.SuccessResult(dto);
    }

    public async Task<ApiResponse<SupplierDto>> CreateSupplierAsync(CreateSupplierDto dto, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.BusinessId;
        if (!_currentUserService.IsSuperAdmin && businessId == null)
        {
            throw new BusinessRuleException("No active business tenant context in current session.");
        }

        var targetBusinessId = businessId ?? (await _context.Businesses.Select(b => b.Id).FirstOrDefaultAsync(cancellationToken));

        string code = dto.SupplierCode?.Trim().ToUpperInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(code))
        {
            var count = await _context.Suppliers.CountAsync(s => s.BusinessId == targetBusinessId, cancellationToken);
            code = $"SUPP-{_dateTimeProvider.UtcNow.Year}-{(count + 1):D4}";
        }

        var exists = await _context.Suppliers
            .AnyAsync(s => s.BusinessId == targetBusinessId && s.SupplierCode == code, cancellationToken);

        if (exists)
        {
            throw new BusinessRuleException($"Supplier with code '{code}' already exists.");
        }

        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            BusinessId = targetBusinessId,
            SupplierCode = code,
            Name = dto.Name.Trim(),
            ContactPerson = dto.ContactPerson?.Trim(),
            Email = dto.Email?.Trim().ToLowerInvariant(),
            Phone = dto.Phone?.Trim(),
            GSTIN = dto.GSTIN?.Trim().ToUpperInvariant(),
            PAN = dto.PAN?.Trim().ToUpperInvariant(),
            BillingAddress = dto.BillingAddress?.Trim(),
            City = dto.City?.Trim(),
            State = dto.State?.Trim(),
            PostalCode = dto.PostalCode?.Trim(),
            Country = dto.Country?.Trim() ?? "India",
            PaymentTermsDays = dto.PaymentTermsDays,
            OutstandingPayable = 0m,
            IsActive = true,
            Notes = dto.Notes
        };

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync(cancellationToken);

        var resultDto = new SupplierDto
        {
            Id = supplier.Id,
            SupplierCode = supplier.SupplierCode,
            Name = supplier.Name,
            ContactPerson = supplier.ContactPerson,
            Email = supplier.Email,
            Phone = supplier.Phone,
            GSTIN = supplier.GSTIN,
            PAN = supplier.PAN,
            BillingAddress = supplier.BillingAddress,
            City = supplier.City,
            State = supplier.State,
            PostalCode = supplier.PostalCode,
            Country = supplier.Country,
            PaymentTermsDays = supplier.PaymentTermsDays,
            OutstandingPayable = supplier.OutstandingPayable,
            IsActive = supplier.IsActive,
            Notes = supplier.Notes,
            CreatedOn = supplier.CreatedOn
        };

        return ApiResponse<SupplierDto>.SuccessResult(resultDto, "Supplier registered successfully.");
    }

    public async Task<ApiResponse<SupplierDto>> UpdateSupplierAsync(Guid id, UpdateSupplierDto dto, CancellationToken cancellationToken = default)
    {
        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (supplier == null)
        {
            throw new NotFoundException("Supplier", id);
        }

        supplier.Name = dto.Name.Trim();
        supplier.ContactPerson = dto.ContactPerson?.Trim();
        supplier.Email = dto.Email?.Trim().ToLowerInvariant();
        supplier.Phone = dto.Phone?.Trim();
        supplier.GSTIN = dto.GSTIN?.Trim().ToUpperInvariant();
        supplier.PAN = dto.PAN?.Trim().ToUpperInvariant();
        supplier.BillingAddress = dto.BillingAddress?.Trim();
        supplier.City = dto.City?.Trim();
        supplier.State = dto.State?.Trim();
        supplier.PostalCode = dto.PostalCode?.Trim();
        supplier.Country = dto.Country?.Trim();
        supplier.PaymentTermsDays = dto.PaymentTermsDays;
        supplier.IsActive = dto.IsActive;
        supplier.Notes = dto.Notes;

        await _context.SaveChangesAsync(cancellationToken);

        var resultDto = new SupplierDto
        {
            Id = supplier.Id,
            SupplierCode = supplier.SupplierCode,
            Name = supplier.Name,
            ContactPerson = supplier.ContactPerson,
            Email = supplier.Email,
            Phone = supplier.Phone,
            GSTIN = supplier.GSTIN,
            PAN = supplier.PAN,
            BillingAddress = supplier.BillingAddress,
            City = supplier.City,
            State = supplier.State,
            PostalCode = supplier.PostalCode,
            Country = supplier.Country,
            PaymentTermsDays = supplier.PaymentTermsDays,
            OutstandingPayable = supplier.OutstandingPayable,
            IsActive = supplier.IsActive,
            Notes = supplier.Notes,
            CreatedOn = supplier.CreatedOn
        };

        return ApiResponse<SupplierDto>.SuccessResult(resultDto, "Supplier updated successfully.");
    }

    public async Task<ApiResponse<bool>> DeleteSupplierAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var supplier = await _context.Suppliers
            .Include(s => s.PurchaseBills)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (supplier == null)
        {
            throw new NotFoundException("Supplier", id);
        }

        if (supplier.PurchaseBills.Any(b => b.BalanceAmount > 0))
        {
            throw new BusinessRuleException("Cannot delete vendor with outstanding unpaid bills.");
        }

        _context.Suppliers.Remove(supplier);
        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResult(true, "Supplier deleted successfully.");
    }
}
