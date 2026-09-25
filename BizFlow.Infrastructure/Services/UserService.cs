using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Users;
using BizFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UserService(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IPasswordHasher passwordHasher,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _passwordHasher = passwordHasher;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApiResponse<PagedResult<UserDto>>> GetUsersAsync(
        int pageNumber = 1, 
        int pageSize = 10, 
        string? search = null, 
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var query = _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(u => 
                u.FirstName.ToLower().Contains(s) || 
                u.LastName.ToLower().Contains(s) || 
                u.Email.ToLower().Contains(s));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderByDescending(u => u.CreatedOn)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserDto
            {
                Id = u.Id,
                BusinessId = u.BusinessId,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                IsActive = u.IsActive,
                LastLoginOn = u.LastLoginOn,
                CreatedOn = u.CreatedOn,
                Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
            })
            .ToListAsync(cancellationToken);

        var pagedResult = PagedResult<UserDto>.Create(users, totalCount, pageNumber, pageSize);
        return ApiResponse<PagedResult<UserDto>>.SuccessResult(pagedResult);
    }

    public async Task<ApiResponse<UserDto>> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("User", id);
        }

        var dto = new UserDto
        {
            Id = user.Id,
            BusinessId = user.BusinessId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            LastLoginOn = user.LastLoginOn,
            CreatedOn = user.CreatedOn,
            Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
        };

        return ApiResponse<UserDto>.SuccessResult(dto);
    }

    public async Task<ApiResponse<UserDto>> CreateUserAsync(CreateUserDto dto, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.BusinessId;
        if (!_currentUserService.IsSuperAdmin && businessId == null)
        {
            throw new BusinessRuleException("Cannot create a user without an active business tenant context.");
        }

        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();

        var emailExists = await _context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (emailExists)
        {
            throw new ValidationException(new List<string> { $"Email '{normalizedEmail}' is already in use." });
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.HashPassword(dto.Password),
            PhoneNumber = dto.PhoneNumber?.Trim(),
            IsActive = true,
            CreatedOn = _dateTimeProvider.UtcNow,
            CreatedBy = _currentUserService.Email ?? "System"
        };

        _context.Users.Add(user);

        // Assign selected roles
        if (dto.RoleIds.Any())
        {
            var validRoles = await _context.Roles
                .Where(r => dto.RoleIds.Contains(r.Id))
                .ToListAsync(cancellationToken);

            foreach (var role in validRoles)
            {
                _context.UserRoles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = role.Id
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return await GetUserByIdAsync(user.Id, cancellationToken);
    }

    public async Task<ApiResponse<UserDto>> UpdateUserAsync(Guid id, UpdateUserDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("User", id);
        }

        user.FirstName = dto.FirstName.Trim();
        user.LastName = dto.LastName.Trim();
        user.PhoneNumber = dto.PhoneNumber?.Trim();
        user.IsActive = dto.IsActive;
        user.UpdatedOn = _dateTimeProvider.UtcNow;
        user.UpdatedBy = _currentUserService.Email ?? "System";

        // Update assigned roles
        _context.UserRoles.RemoveRange(user.UserRoles);

        if (dto.RoleIds.Any())
        {
            var validRoles = await _context.Roles
                .Where(r => dto.RoleIds.Contains(r.Id))
                .ToListAsync(cancellationToken);

            foreach (var role in validRoles)
            {
                _context.UserRoles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = role.Id
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return await GetUserByIdAsync(user.Id, cancellationToken);
    }

    public async Task<ApiResponse> DeleteUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("User", id);
        }

        if (user.Id == _currentUserService.UserId)
        {
            throw new BusinessRuleException("You cannot delete your own active user account.");
        }

        _context.Users.Remove(user); // Triggers soft-delete in ApplicationDbContext
        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse.SuccessResult("User removed successfully.");
    }
}
