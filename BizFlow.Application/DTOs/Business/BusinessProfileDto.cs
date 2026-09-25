namespace BizFlow.Application.DTOs.Business;

public class BusinessProfileDto
{
    public Guid Id { get; set; }
    public string BusinessCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? GSTNumber { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string Currency { get; set; } = "INR";
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
}

public class UpdateBusinessProfileDto
{
    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? GSTNumber { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string Currency { get; set; } = "INR";
}
