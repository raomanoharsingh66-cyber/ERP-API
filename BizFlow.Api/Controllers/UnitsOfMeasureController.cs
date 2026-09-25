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

public class UnitsOfMeasureController : BaseApiController
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UnitsOfMeasureController(
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
    public async Task<ActionResult<ApiResponse<List<UnitOfMeasureDto>>>> GetUnitsOfMeasure(CancellationToken cancellationToken)
    {
        var uoms = await _context.UnitsOfMeasure
            .AsNoTracking()
            .OrderBy(u => u.Name)
            .Select(u => new UnitOfMeasureDto
            {
                Id = u.Id,
                Name = u.Name,
                Code = u.Code,
                Description = u.Description,
                IsActive = u.IsActive
            })
            .ToListAsync(cancellationToken);

        return OkResponse(uoms);
    }

    [HttpPost]
    [HasPermission(Permissions.Inventory.Create)]
    public async Task<ActionResult<ApiResponse<UnitOfMeasureDto>>> CreateUnitOfMeasure(
        [FromBody] CreateUnitOfMeasureDto dto, 
        CancellationToken cancellationToken)
    {
        var businessId = _currentUserService.BusinessId;
        if (!_currentUserService.IsSuperAdmin && businessId == null)
        {
            throw new BusinessRuleException("No active business tenant context in current session.");
        }

        var normalizedCode = dto.Code.Trim().ToUpperInvariant();

        var exists = await _context.UnitsOfMeasure
            .AnyAsync(u => u.Code == normalizedCode, cancellationToken);

        if (exists)
        {
            throw new ValidationException(new List<string> { $"Unit of measure '{normalizedCode}' already exists." });
        }

        var uom = new UnitOfMeasure
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId!.Value,
            Name = dto.Name.Trim(),
            Code = normalizedCode,
            Description = dto.Description?.Trim(),
            IsActive = true,
            CreatedOn = _dateTimeProvider.UtcNow,
            CreatedBy = _currentUserService.Email ?? "System"
        };

        _context.UnitsOfMeasure.Add(uom);
        await _context.SaveChangesAsync(cancellationToken);

        var resultDto = new UnitOfMeasureDto
        {
            Id = uom.Id,
            Name = uom.Name,
            Code = uom.Code,
            Description = uom.Description,
            IsActive = uom.IsActive
        };

        return OkResponse(resultDto, "Unit of Measure created successfully.");
    }
}
