using System.Security.Claims;
using BizFlow.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace BizFlow.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var idClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                          ?? User?.FindFirst("sub")?.Value;

            return Guid.TryParse(idClaim, out var userId) ? userId : null;
        }
    }

    public Guid? BusinessId
    {
        get
        {
            var businessClaim = User?.FindFirst("business_id")?.Value;
            if (Guid.TryParse(businessClaim, out var businessId) && businessId != Guid.Empty)
            {
                return businessId;
            }

            var headerVal = _httpContextAccessor.HttpContext?.Request.Headers["X-Business-Id"].FirstOrDefault();
            if (Guid.TryParse(headerVal, out var headerId) && headerId != Guid.Empty)
            {
                return headerId;
            }

            return null;
        }
    }

    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value 
                           ?? User?.FindFirst("email")?.Value;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public List<string> Roles => User?.FindAll(ClaimTypes.Role)
        .Select(c => c.Value)
        .Distinct()
        .ToList() ?? new List<string>();

    public List<string> Permissions => User?.FindAll("permission")
        .Select(c => c.Value)
        .Distinct()
        .ToList() ?? new List<string>();

    public bool IsSuperAdmin => Roles.Contains("SuperAdmin", StringComparer.OrdinalIgnoreCase);
}
