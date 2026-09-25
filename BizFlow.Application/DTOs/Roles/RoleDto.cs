namespace BizFlow.Application.DTOs.Roles;

public class RoleDto
{
    public Guid Id { get; set; }
    public Guid? BusinessId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public bool IsActive { get; set; }
    public List<PermissionDto> Permissions { get; set; } = new();
}

public class CreateRoleDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<Guid> PermissionIds { get; set; } = new();
}

public class UpdateRolePermissionsDto
{
    public List<Guid> PermissionIds { get; set; } = new();
}
