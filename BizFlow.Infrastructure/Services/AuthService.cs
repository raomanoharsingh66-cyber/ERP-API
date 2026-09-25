using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Auth;
using BizFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AuthService(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApiResponse<LoginResponseDto>> RegisterBusinessAsync(
        RegisterBusinessDto dto, 
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = dto.BusinessCode.Trim().ToUpperInvariant();
        var normalizedEmail = dto.AdminEmail.Trim().ToLowerInvariant();

        // 1. Verify business code uniqueness
        var businessExists = await _context.Businesses
            .IgnoreQueryFilters()
            .AnyAsync(b => b.BusinessCode == normalizedCode, cancellationToken);

        if (businessExists)
        {
            throw new ValidationException(new List<string> { $"Business Code '{normalizedCode}' is already registered." });
        }

        // 2. Verify admin email uniqueness
        var userExists = await _context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (userExists)
        {
            throw new ValidationException(new List<string> { $"Email '{normalizedEmail}' is already associated with an account." });
        }

        // 3. Create Business Entity
        var business = new Business
        {
            Id = Guid.NewGuid(),
            BusinessCode = normalizedCode,
            Name = dto.BusinessName.Trim(),
            LegalName = dto.LegalName?.Trim(),
            GSTNumber = dto.GSTNumber?.Trim(),
            Email = dto.BusinessEmail.Trim(),
            Phone = dto.BusinessPhone?.Trim(),
            Address = dto.Address?.Trim(),
            Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "INR" : dto.Currency.Trim().ToUpperInvariant(),
            IsActive = true,
            CreatedOn = _dateTimeProvider.UtcNow,
            CreatedBy = normalizedEmail
        };

        _context.Businesses.Add(business);

        // 4. Create default tenant Administrator role
        var adminRole = new Role
        {
            Id = Guid.NewGuid(),
            BusinessId = business.Id,
            Name = "BusinessAdmin",
            Description = "Full administrator privileges for tenant business operations.",
            IsSystemRole = false,
            IsActive = true,
            CreatedOn = _dateTimeProvider.UtcNow,
            CreatedBy = normalizedEmail
        };

        _context.Roles.Add(adminRole);

        // Attach all existing system permissions to this tenant Admin
        var allPermissions = await _context.Permissions.ToListAsync(cancellationToken);
        foreach (var permission in allPermissions)
        {
            _context.RolePermissions.Add(new RolePermission
            {
                RoleId = adminRole.Id,
                PermissionId = permission.Id
            });
        }

        // 5. Create default Standard roles (Manager, Accountant, SalesExecutive, StoreKeeper)
        var managerRole = new Role
        {
            Id = Guid.NewGuid(),
            BusinessId = business.Id,
            Name = "Manager",
            Description = "Operational manager with sales, purchases, and inventory access.",
            IsSystemRole = false,
            IsActive = true,
            CreatedOn = _dateTimeProvider.UtcNow,
            CreatedBy = normalizedEmail
        };
        _context.Roles.Add(managerRole);

        // 6. Create Primary Admin User
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            BusinessId = business.Id,
            FirstName = dto.AdminFirstName.Trim(),
            LastName = dto.AdminLastName.Trim(),
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.HashPassword(dto.AdminPassword),
            PhoneNumber = dto.AdminPhoneNumber?.Trim(),
            IsActive = true,
            CreatedOn = _dateTimeProvider.UtcNow,
            CreatedBy = normalizedEmail
        };

        _context.Users.Add(adminUser);

        // Map User to BusinessAdmin role
        _context.UserRoles.Add(new UserRole
        {
            UserId = adminUser.Id,
            RoleId = adminRole.Id
        });

        // Save all changes atomically
        await _context.SaveChangesAsync(cancellationToken);

        // 7. Issue JWT token pair for immediate login
        var roles = new List<string> { adminRole.Name };
        var permissionCodes = allPermissions.Select(p => p.Code).ToList();

        var accessToken = _jwtTokenService.GenerateAccessToken(adminUser, roles, permissionCodes);
        var refreshToken = _jwtTokenService.GenerateRefreshToken(adminUser.Id);

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        var userDto = new CurrentUserDto
        {
            Id = adminUser.Id,
            BusinessId = business.Id,
            Email = adminUser.Email,
            FirstName = adminUser.FirstName,
            LastName = adminUser.LastName,
            IsSuperAdmin = false,
            Roles = roles,
            Permissions = permissionCodes
        };

        var responseData = new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresIn = 3600,
            User = userDto
        };

        return ApiResponse<LoginResponseDto>.SuccessResult(responseData, "Business and Administrator registered successfully.");
    }

    public async Task<ApiResponse<LoginResponseDto>> LoginAsync(
        LoginRequestDto dto, 
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .IgnoreQueryFilters()
            .Include(u => u.Business)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail && !u.IsDeleted, cancellationToken);

        if (user == null || !user.IsActive)
        {
            return ApiResponse<LoginResponseDto>.FailureResult("Invalid email or password.");
        }

        if (user.Business != null && !user.Business.IsActive)
        {
            return ApiResponse<LoginResponseDto>.FailureResult("The associated business tenant has been deactivated.");
        }

        var isPasswordValid = _passwordHasher.VerifyPassword(dto.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            return ApiResponse<LoginResponseDto>.FailureResult("Invalid email or password.");
        }

        // Extract assigned roles & distinct permissions
        var roles = user.UserRoles
            .Where(ur => ur.Role.IsActive && !ur.Role.IsDeleted)
            .Select(ur => ur.Role.Name)
            .Distinct()
            .ToList();

        var permissions = user.UserRoles
            .Where(ur => ur.Role.IsActive && !ur.Role.IsDeleted)
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToList();

        // Update LastLogin
        user.LastLoginOn = _dateTimeProvider.UtcNow;

        if (user.BusinessId == null)
        {
            var defaultBusiness = await _context.Businesses
                .IgnoreQueryFilters()
                .OrderBy(b => b.CreatedOn)
                .FirstOrDefaultAsync(cancellationToken);

            if (defaultBusiness != null)
            {
                user.BusinessId = defaultBusiness.Id;
            }
        }

        var accessToken = _jwtTokenService.GenerateAccessToken(user, roles, permissions);
        var refreshToken = _jwtTokenService.GenerateRefreshToken(user.Id);

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        var userDto = new CurrentUserDto
        {
            Id = user.Id,
            BusinessId = user.BusinessId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsSuperAdmin = roles.Contains("SuperAdmin", StringComparer.OrdinalIgnoreCase),
            Roles = roles,
            Permissions = permissions
        };

        var responseData = new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresIn = 3600,
            User = userDto
        };

        return ApiResponse<LoginResponseDto>.SuccessResult(responseData, "Login successful.");
    }

    public async Task<ApiResponse<LoginResponseDto>> RefreshTokenAsync(
        RefreshTokenRequestDto dto, 
        CancellationToken cancellationToken = default)
    {
        var existingToken = await _context.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(rt => rt.Token == dto.RefreshToken, cancellationToken);

        if (existingToken == null || !existingToken.IsActive)
        {
            return ApiResponse<LoginResponseDto>.FailureResult("Refresh token is expired, invalid, or revoked.");
        }

        var user = existingToken.User;
        if (!user.IsActive || user.IsDeleted)
        {
            return ApiResponse<LoginResponseDto>.FailureResult("User account is inactive.");
        }

        // Rotate token
        existingToken.IsUsed = true;

        var roles = user.UserRoles
            .Where(ur => ur.Role.IsActive && !ur.Role.IsDeleted)
            .Select(ur => ur.Role.Name)
            .Distinct()
            .ToList();

        var permissions = user.UserRoles
            .Where(ur => ur.Role.IsActive && !ur.Role.IsDeleted)
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToList();

        var newAccessToken = _jwtTokenService.GenerateAccessToken(user, roles, permissions);
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken(user.Id);

        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        var userDto = new CurrentUserDto
        {
            Id = user.Id,
            BusinessId = user.BusinessId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsSuperAdmin = roles.Contains("SuperAdmin", StringComparer.OrdinalIgnoreCase),
            Roles = roles,
            Permissions = permissions
        };

        var responseData = new LoginResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken.Token,
            ExpiresIn = 3600,
            User = userDto
        };

        return ApiResponse<LoginResponseDto>.SuccessResult(responseData, "Token refreshed successfully.");
    }

    public async Task<ApiResponse> RevokeTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var existingToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);

        if (existingToken != null)
        {
            existingToken.IsRevoked = true;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return ApiResponse.SuccessResult("Token revoked successfully.");
    }

    public async Task<ApiResponse<CurrentUserDto>> GetCurrentUserProfileAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.UserId;
        if (currentUserId == null)
        {
            throw new AppException("User session is not authenticated.", System.Net.HttpStatusCode.Unauthorized);
        }

        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == currentUserId.Value, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("User", currentUserId.Value);
        }

        var roles = user.UserRoles
            .Where(ur => ur.Role.IsActive && !ur.Role.IsDeleted)
            .Select(ur => ur.Role.Name)
            .Distinct()
            .ToList();

        var permissions = user.UserRoles
            .Where(ur => ur.Role.IsActive && !ur.Role.IsDeleted)
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToList();

        var userDto = new CurrentUserDto
        {
            Id = user.Id,
            BusinessId = user.BusinessId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsSuperAdmin = roles.Contains("SuperAdmin", StringComparer.OrdinalIgnoreCase),
            Roles = roles,
            Permissions = permissions
        };

        return ApiResponse<CurrentUserDto>.SuccessResult(userDto);
    }
}
