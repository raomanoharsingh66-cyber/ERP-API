using BizFlow.Api.Attributes;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Roles;
using BizFlow.Domain.Constants;
using BizFlow.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Api.Controllers;

public class RolesController : BaseApiController
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RolesController(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    [HttpGet]
    [HasPermission(Permissions.Roles.View)]
    public async Task<ActionResult<ApiResponse<List<RoleDto>>>> GetRoles(CancellationToken cancellationToken)
    {
        var roles = await _context.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .AsNoTracking()
            .Select(r => new RoleDto
            {
                Id = r.Id,
                BusinessId = r.BusinessId,
                Name = r.Name,
                Description = r.Description,
                IsSystemRole = r.IsSystemRole,
                IsActive = r.IsActive,
                Permissions = r.RolePermissions.Select(rp => new PermissionDto
                {
                    Id = rp.Permission.Id,
                    Code = rp.Permission.Code,
                    Module = rp.Permission.Module,
                    Description = rp.Permission.Description
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        return OkResponse(roles);
    }

    [HttpGet("permissions")]
    [HasPermission(Permissions.Roles.View)]
    public async Task<ActionResult<ApiResponse<List<PermissionDto>>>> GetAllPermissions(CancellationToken cancellationToken)
    {
        var permissions = await _context.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Module)
            .ThenBy(p => p.Code)
            .Select(p => new PermissionDto
            {
                Id = p.Id,
                Code = p.Code,
                Module = p.Module,
                Description = p.Description
            })
            .ToListAsync(cancellationToken);

        return OkResponse(permissions);
    }

    [HttpPost]
    [HasPermission(Permissions.Roles.Create)]
    public async Task<ActionResult<ApiResponse<RoleDto>>> CreateRole(
        [FromBody] CreateRoleDto dto, 
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new ValidationException(new List<string> { "Role name is required." });
        }

        var businessId = _currentUserService.BusinessId;
        var roleName = dto.Name.Trim();

        var exists = await _context.Roles
            .AnyAsync(r => r.Name == roleName, cancellationToken);

        if (exists)
        {
            throw new ValidationException(new List<string> { $"Role '{roleName}' already exists." });
        }

        var role = new Role
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Name = roleName,
            Description = dto.Description?.Trim(),
            IsSystemRole = false,
            IsActive = true,
            CreatedOn = _dateTimeProvider.UtcNow,
            CreatedBy = _currentUserService.Email ?? "System"
        };

        _context.Roles.Add(role);

        if (dto.PermissionIds.Any())
        {
            var validPermissions = await _context.Permissions
                .Where(p => dto.PermissionIds.Contains(p.Id))
                .ToListAsync(cancellationToken);

            foreach (var perm in validPermissions)
            {
                _context.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = perm.Id
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return await GetRoleById(role.Id, cancellationToken);
    }

    [HttpPut("{id:guid}/permissions")]
    [HasPermission(Permissions.Roles.Update)]
    public async Task<ActionResult<ApiResponse<RoleDto>>> UpdateRolePermissions(
        Guid id, 
        [FromBody] UpdateRolePermissionsDto dto, 
        CancellationToken cancellationToken)
    {
        var role = await _context.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (role == null)
        {
            throw new NotFoundException("Role", id);
        }

        if (role.IsSystemRole && !_currentUserService.IsSuperAdmin)
        {
            throw new BusinessRuleException("System roles cannot be modified by tenant administrators.");
        }

        _context.RolePermissions.RemoveRange(role.RolePermissions);

        if (dto.PermissionIds.Any())
        {
            var validPermissions = await _context.Permissions
                .Where(p => dto.PermissionIds.Contains(p.Id))
                .ToListAsync(cancellationToken);

            foreach (var perm in validPermissions)
            {
                _context.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = perm.Id
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return await GetRoleById(role.Id, cancellationToken);
    }

    private async Task<ActionResult<ApiResponse<RoleDto>>> GetRoleById(Guid id, CancellationToken cancellationToken)
    {
        var role = await _context.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (role == null)
        {
            throw new NotFoundException("Role", id);
        }

        var dto = new RoleDto
        {
            Id = role.Id,
            BusinessId = role.BusinessId,
            Name = role.Name,
            Description = role.Description,
            IsSystemRole = role.IsSystemRole,
            IsActive = role.IsActive,
            Permissions = role.RolePermissions.Select(rp => new PermissionDto
            {
                Id = rp.Permission.Id,
                Code = rp.Permission.Code,
                Module = rp.Permission.Module,
                Description = rp.Permission.Description
            }).ToList()
        };

        return OkResponse(dto);
    }
}
