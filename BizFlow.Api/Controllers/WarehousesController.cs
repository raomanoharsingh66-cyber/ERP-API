using BizFlow.Api.Attributes;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Inventory;
using BizFlow.Domain.Constants;
using BizFlow.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Api.Controllers;

public class WarehousesController : BaseApiController
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public WarehousesController(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    [HttpGet]
    [HasPermission(Permissions.Inventory.View)]
    public async Task<ActionResult<ApiResponse<List<WarehouseDto>>>> GetWarehouses(CancellationToken cancellationToken)
    {
        var warehouses = await _context.Warehouses
            .Include(w => w.Stocks)
            .AsNoTracking()
            .OrderByDescending(w => w.IsDefault)
            .ThenBy(w => w.Name)
            .Select(w => new WarehouseDto
            {
                Id = w.Id,
                Name = w.Name,
                Code = w.Code,
                Address = w.Address,
                ContactPerson = w.ContactPerson,
                Phone = w.Phone,
                IsDefault = w.IsDefault,
                IsActive = w.IsActive,
                UniqueItemsCount = w.Stocks.Count(s => !s.IsDeleted && s.QuantityOnHand > 0),
                TotalQuantityOnHand = w.Stocks.Where(s => !s.IsDeleted).Sum(s => s.QuantityOnHand)
            })
            .ToListAsync(cancellationToken);

        return OkResponse(warehouses);
    }

    [HttpPost]
    [HasPermission(Permissions.Inventory.Create)]
    public async Task<ActionResult<ApiResponse<WarehouseDto>>> CreateWarehouse(
        [FromBody] CreateWarehouseDto dto, 
        CancellationToken cancellationToken)
    {
        var businessId = _currentUserService.BusinessId;
        if (!_currentUserService.IsSuperAdmin && businessId == null)
        {
            throw new BusinessRuleException("No active business tenant context in current session.");
        }

        var normalizedCode = dto.Code.Trim().ToUpperInvariant();

        var exists = await _context.Warehouses
            .AnyAsync(w => w.Code == normalizedCode, cancellationToken);

        if (exists)
        {
            throw new ValidationException(new List<string> { $"Warehouse with code '{normalizedCode}' already exists." });
        }

        // If setting as default, unset existing default
        if (dto.IsDefault)
        {
            var currentDefault = await _context.Warehouses.FirstOrDefaultAsync(w => w.IsDefault, cancellationToken);
            if (currentDefault != null)
            {
                currentDefault.IsDefault = false;
            }
        }

        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId!.Value,
            Name = dto.Name.Trim(),
            Code = normalizedCode,
            Address = dto.Address?.Trim(),
            ContactPerson = dto.ContactPerson?.Trim(),
            Phone = dto.Phone?.Trim(),
            IsDefault = dto.IsDefault,
            IsActive = true,
            CreatedOn = _dateTimeProvider.UtcNow,
            CreatedBy = _currentUserService.Email ?? "System"
        };

        _context.Warehouses.Add(warehouse);
        await _context.SaveChangesAsync(cancellationToken);

        var resultDto = new WarehouseDto
        {
            Id = warehouse.Id,
            Name = warehouse.Name,
            Code = warehouse.Code,
            Address = warehouse.Address,
            ContactPerson = warehouse.ContactPerson,
            Phone = warehouse.Phone,
            IsDefault = warehouse.IsDefault,
            IsActive = warehouse.IsActive
        };

        return OkResponse(resultDto, "Warehouse created successfully.");
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Inventory.Update)]
    public async Task<ActionResult<ApiResponse<WarehouseDto>>> UpdateWarehouse(
        Guid id, 
        [FromBody] UpdateWarehouseDto dto, 
        CancellationToken cancellationToken)
    {
        var warehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (warehouse == null)
        {
            throw new NotFoundException("Warehouse", id);
        }

        if (dto.IsDefault && !warehouse.IsDefault)
        {
            var currentDefault = await _context.Warehouses.FirstOrDefaultAsync(w => w.IsDefault && w.Id != id, cancellationToken);
            if (currentDefault != null)
            {
                currentDefault.IsDefault = false;
            }
        }

        warehouse.Name = dto.Name.Trim();
        warehouse.Address = dto.Address?.Trim();
        warehouse.ContactPerson = dto.ContactPerson?.Trim();
        warehouse.Phone = dto.Phone?.Trim();
        warehouse.IsDefault = dto.IsDefault;
        warehouse.IsActive = dto.IsActive;
        warehouse.UpdatedOn = _dateTimeProvider.UtcNow;
        warehouse.UpdatedBy = _currentUserService.Email ?? "System";

        await _context.SaveChangesAsync(cancellationToken);

        var resultDto = new WarehouseDto
        {
            Id = warehouse.Id,
            Name = warehouse.Name,
            Code = warehouse.Code,
            Address = warehouse.Address,
            ContactPerson = warehouse.ContactPerson,
            Phone = warehouse.Phone,
            IsDefault = warehouse.IsDefault,
            IsActive = warehouse.IsActive
        };

        return OkResponse(resultDto, "Warehouse updated successfully.");
    }
}
