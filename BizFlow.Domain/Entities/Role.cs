using BizFlow.Domain.Common;

namespace BizFlow.Domain.Entities;

public class Role : BaseEntity
{
    public Guid? BusinessId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; } = false;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Business? Business { get; set; }
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
