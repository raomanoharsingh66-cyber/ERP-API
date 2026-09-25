using BizFlow.Domain.Common;

namespace BizFlow.Domain.Entities;

public class Business : BaseEntity
{
    public string BusinessCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? GSTNumber { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string Currency { get; set; } = "INR";
    public bool IsActive { get; set; } = true;

    // Navigation collections
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Role> Roles { get; set; } = new List<Role>();
}
