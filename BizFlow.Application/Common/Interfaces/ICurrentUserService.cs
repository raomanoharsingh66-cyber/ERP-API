namespace BizFlow.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    Guid? BusinessId { get; }
    string? Email { get; }
    bool IsSuperAdmin { get; }
    bool IsAuthenticated { get; }
    List<string> Roles { get; }
    List<string> Permissions { get; }
}
