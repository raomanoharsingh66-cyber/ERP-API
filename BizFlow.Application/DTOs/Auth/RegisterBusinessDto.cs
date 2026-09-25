namespace BizFlow.Application.DTOs.Auth;

public class RegisterBusinessDto
{
    // Business info
    public string BusinessName { get; set; } = string.Empty;
    public string BusinessCode { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? GSTNumber { get; set; }
    public string BusinessEmail { get; set; } = string.Empty;
    public string? BusinessPhone { get; set; }
    public string? Address { get; set; }
    public string Currency { get; set; } = "INR";

    // Primary Admin User
    public string AdminFirstName { get; set; } = string.Empty;
    public string AdminLastName { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
    public string? AdminPhoneNumber { get; set; }
}
