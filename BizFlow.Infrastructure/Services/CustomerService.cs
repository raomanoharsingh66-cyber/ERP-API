using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Sales;
using BizFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Services;

public class CustomerService : ICustomerService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CustomerService(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApiResponse<PagedResult<CustomerDto>>> GetCustomersAsync(
        string? search = null,
        bool? isActive = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(c => 
                c.Name.ToLower().Contains(s) || 
                c.CustomerCode.ToLower().Contains(s) || 
                (c.Email != null && c.Email.ToLower().Contains(s)) ||
                (c.Phone != null && c.Phone.Contains(s)) ||
                (c.GSTIN != null && c.GSTIN.ToLower().Contains(s)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(c => c.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(c => c.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CustomerDto
            {
                Id = c.Id,
                CustomerCode = c.CustomerCode,
                Name = c.Name,
                Email = c.Email,
                Phone = c.Phone,
                GSTIN = c.GSTIN,
                PAN = c.PAN,
                BillingAddress = c.BillingAddress,
                BillingCity = c.BillingCity,
                BillingState = c.BillingState,
                BillingPostalCode = c.BillingPostalCode,
                BillingCountry = c.BillingCountry,
                ShippingAddress = c.ShippingAddress,
                ShippingCity = c.ShippingCity,
                ShippingState = c.ShippingState,
                ShippingPostalCode = c.ShippingPostalCode,
                ShippingCountry = c.ShippingCountry,
                CreditLimit = c.CreditLimit,
                OutstandingBalance = c.OutstandingBalance,
                IsActive = c.IsActive,
                Notes = c.Notes,
                CreatedOn = c.CreatedOn
            })
            .ToListAsync(cancellationToken);

        var result = PagedResult<CustomerDto>.Create(items, totalCount, pageNumber, pageSize);
        return ApiResponse<PagedResult<CustomerDto>>.SuccessResult(result);
    }

    public async Task<ApiResponse<CustomerDto>> GetCustomerByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var c = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (c == null)
        {
            throw new NotFoundException("Customer", id);
        }

        var dto = new CustomerDto
        {
            Id = c.Id,
            CustomerCode = c.CustomerCode,
            Name = c.Name,
            Email = c.Email,
            Phone = c.Phone,
            GSTIN = c.GSTIN,
            PAN = c.PAN,
            BillingAddress = c.BillingAddress,
            BillingCity = c.BillingCity,
            BillingState = c.BillingState,
            BillingPostalCode = c.BillingPostalCode,
            BillingCountry = c.BillingCountry,
            ShippingAddress = c.ShippingAddress,
            ShippingCity = c.ShippingCity,
            ShippingState = c.ShippingState,
            ShippingPostalCode = c.ShippingPostalCode,
            ShippingCountry = c.ShippingCountry,
            CreditLimit = c.CreditLimit,
            OutstandingBalance = c.OutstandingBalance,
            IsActive = c.IsActive,
            Notes = c.Notes,
            CreatedOn = c.CreatedOn
        };

        return ApiResponse<CustomerDto>.SuccessResult(dto);
    }

    public async Task<ApiResponse<CustomerDto>> CreateCustomerAsync(CreateCustomerDto dto, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.BusinessId;
        if (!_currentUserService.IsSuperAdmin && businessId == null)
        {
            throw new BusinessRuleException("No active business tenant context in current session.");
        }

        var targetBusinessId = businessId ?? (await _context.Businesses.Select(b => b.Id).FirstOrDefaultAsync(cancellationToken));

        // Auto generate customer code if not supplied
        string code = dto.CustomerCode?.Trim().ToUpperInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(code))
        {
            var count = await _context.Customers.CountAsync(c => c.BusinessId == targetBusinessId, cancellationToken);
            code = $"CUST-{_dateTimeProvider.UtcNow.Year}-{(count + 1):D4}";
        }

        var exists = await _context.Customers
            .AnyAsync(c => c.BusinessId == targetBusinessId && c.CustomerCode == code, cancellationToken);

        if (exists)
        {
            throw new BusinessRuleException($"Customer with code '{code}' already exists.");
        }

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            BusinessId = targetBusinessId,
            CustomerCode = code,
            Name = dto.Name.Trim(),
            Email = dto.Email?.Trim().ToLowerInvariant(),
            Phone = dto.Phone?.Trim(),
            GSTIN = dto.GSTIN?.Trim().ToUpperInvariant(),
            PAN = dto.PAN?.Trim().ToUpperInvariant(),
            BillingAddress = dto.BillingAddress?.Trim(),
            BillingCity = dto.BillingCity?.Trim(),
            BillingState = dto.BillingState?.Trim(),
            BillingPostalCode = dto.BillingPostalCode?.Trim(),
            BillingCountry = dto.BillingCountry?.Trim() ?? "India",
            ShippingAddress = dto.ShippingAddress?.Trim() ?? dto.BillingAddress?.Trim(),
            ShippingCity = dto.ShippingCity?.Trim() ?? dto.BillingCity?.Trim(),
            ShippingState = dto.ShippingState?.Trim() ?? dto.BillingState?.Trim(),
            ShippingPostalCode = dto.ShippingPostalCode?.Trim() ?? dto.BillingPostalCode?.Trim(),
            ShippingCountry = dto.ShippingCountry?.Trim() ?? dto.BillingCountry?.Trim() ?? "India",
            CreditLimit = dto.CreditLimit,
            OutstandingBalance = 0m,
            IsActive = true,
            Notes = dto.Notes
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync(cancellationToken);

        var resultDto = new CustomerDto
        {
            Id = customer.Id,
            CustomerCode = customer.CustomerCode,
            Name = customer.Name,
            Email = customer.Email,
            Phone = customer.Phone,
            GSTIN = customer.GSTIN,
            PAN = customer.PAN,
            BillingAddress = customer.BillingAddress,
            BillingCity = customer.BillingCity,
            BillingState = customer.BillingState,
            BillingPostalCode = customer.BillingPostalCode,
            BillingCountry = customer.BillingCountry,
            ShippingAddress = customer.ShippingAddress,
            ShippingCity = customer.ShippingCity,
            ShippingState = customer.ShippingState,
            ShippingPostalCode = customer.ShippingPostalCode,
            ShippingCountry = customer.ShippingCountry,
            CreditLimit = customer.CreditLimit,
            OutstandingBalance = customer.OutstandingBalance,
            IsActive = customer.IsActive,
            Notes = customer.Notes,
            CreatedOn = customer.CreatedOn
        };

        return ApiResponse<CustomerDto>.SuccessResult(resultDto, "Customer created successfully.");
    }

    public async Task<ApiResponse<CustomerDto>> UpdateCustomerAsync(Guid id, UpdateCustomerDto dto, CancellationToken cancellationToken = default)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (customer == null)
        {
            throw new NotFoundException("Customer", id);
        }

        customer.Name = dto.Name.Trim();
        customer.Email = dto.Email?.Trim().ToLowerInvariant();
        customer.Phone = dto.Phone?.Trim();
        customer.GSTIN = dto.GSTIN?.Trim().ToUpperInvariant();
        customer.PAN = dto.PAN?.Trim().ToUpperInvariant();
        customer.BillingAddress = dto.BillingAddress?.Trim();
        customer.BillingCity = dto.BillingCity?.Trim();
        customer.BillingState = dto.BillingState?.Trim();
        customer.BillingPostalCode = dto.BillingPostalCode?.Trim();
        customer.BillingCountry = dto.BillingCountry?.Trim();
        customer.ShippingAddress = dto.ShippingAddress?.Trim();
        customer.ShippingCity = dto.ShippingCity?.Trim();
        customer.ShippingState = dto.ShippingState?.Trim();
        customer.ShippingPostalCode = dto.ShippingPostalCode?.Trim();
        customer.ShippingCountry = dto.ShippingCountry?.Trim();
        customer.CreditLimit = dto.CreditLimit;
        customer.IsActive = dto.IsActive;
        customer.Notes = dto.Notes;

        await _context.SaveChangesAsync(cancellationToken);

        var resultDto = new CustomerDto
        {
            Id = customer.Id,
            CustomerCode = customer.CustomerCode,
            Name = customer.Name,
            Email = customer.Email,
            Phone = customer.Phone,
            GSTIN = customer.GSTIN,
            PAN = customer.PAN,
            BillingAddress = customer.BillingAddress,
            BillingCity = customer.BillingCity,
            BillingState = customer.BillingState,
            BillingPostalCode = customer.BillingPostalCode,
            BillingCountry = customer.BillingCountry,
            ShippingAddress = customer.ShippingAddress,
            ShippingCity = customer.ShippingCity,
            ShippingState = customer.ShippingState,
            ShippingPostalCode = customer.ShippingPostalCode,
            ShippingCountry = customer.ShippingCountry,
            CreditLimit = customer.CreditLimit,
            OutstandingBalance = customer.OutstandingBalance,
            IsActive = customer.IsActive,
            Notes = customer.Notes,
            CreatedOn = customer.CreatedOn
        };

        return ApiResponse<CustomerDto>.SuccessResult(resultDto, "Customer updated successfully.");
    }

    public async Task<ApiResponse<bool>> DeleteCustomerAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await _context.Customers
            .Include(c => c.Invoices)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (customer == null)
        {
            throw new NotFoundException("Customer", id);
        }

        if (customer.Invoices.Any(i => i.BalanceAmount > 0))
        {
            throw new BusinessRuleException("Cannot delete customer with unpaid invoices or outstanding balance.");
        }

        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResult(true, "Customer deleted successfully.");
    }
}
