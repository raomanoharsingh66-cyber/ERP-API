namespace BizFlow.Application.DTOs.Users;

public class UserDto
{
    public Guid Id { get; set; }
    public Guid? BusinessId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset? LastLoginOn { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public List<string> Roles { get; set; } = new();
}
