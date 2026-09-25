using BizFlow.Api.Attributes;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Business;
using BizFlow.Domain.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Api.Controllers;

public class BusinessController : BaseApiController
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public BusinessController(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    [HttpGet("profile")]
    [HasPermission(Permissions.Business.View)]
    public async Task<ActionResult<ApiResponse<BusinessProfileDto>>> GetBusinessProfile(CancellationToken cancellationToken)
    {
        var businessId = _currentUserService.BusinessId 
            ?? await _context.Businesses.OrderBy(b => b.CreatedOn).Select(b => (Guid?)b.Id).FirstOrDefaultAsync(cancellationToken);

        if (businessId == null)
        {
            throw new BusinessRuleException("No active business tenant context in current session.");
        }

        var business = await _context.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == businessId.Value, cancellationToken);

        if (business == null)
        {
            throw new NotFoundException("Business", businessId.Value);
        }

        var dto = new BusinessProfileDto
        {
            Id = business.Id,
            BusinessCode = business.BusinessCode,
            Name = business.Name,
            LegalName = business.LegalName,
            GSTNumber = business.GSTNumber,
            Email = business.Email,
            Phone = business.Phone,
            Address = business.Address,
            Currency = business.Currency,
            IsActive = business.IsActive,
            CreatedOn = business.CreatedOn
        };

        return OkResponse(dto);
    }

    [HttpPut("profile")]
    [HasPermission(Permissions.Business.Update)]
    public async Task<ActionResult<ApiResponse<BusinessProfileDto>>> UpdateBusinessProfile(
        [FromBody] UpdateBusinessProfileDto dto, 
        CancellationToken cancellationToken)
    {
        var businessId = _currentUserService.BusinessId 
            ?? await _context.Businesses.OrderBy(b => b.CreatedOn).Select(b => (Guid?)b.Id).FirstOrDefaultAsync(cancellationToken);

        if (businessId == null)
        {
            throw new BusinessRuleException("No active business tenant context in current session.");
        }

        var business = await _context.Businesses
            .FirstOrDefaultAsync(b => b.Id == businessId.Value, cancellationToken);

        if (business == null)
        {
            throw new NotFoundException("Business", businessId.Value);
        }

        business.Name = dto.Name.Trim();
        business.LegalName = dto.LegalName?.Trim();
        business.GSTNumber = dto.GSTNumber?.Trim();
        business.Email = dto.Email?.Trim();
        business.Phone = dto.Phone?.Trim();
        business.Address = dto.Address?.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Currency))
        {
            business.Currency = dto.Currency.Trim().ToUpperInvariant();
        }
        business.UpdatedOn = _dateTimeProvider.UtcNow;
        business.UpdatedBy = _currentUserService.Email ?? "System";

        await _context.SaveChangesAsync(cancellationToken);

        return await GetBusinessProfile(cancellationToken);
    }
}
