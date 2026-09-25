namespace BizFlow.Domain.Entities;

public class Permission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Navigation collection
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
