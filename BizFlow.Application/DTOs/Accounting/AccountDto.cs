using BizFlow.Domain.Enums;

namespace BizFlow.Application.DTOs.Accounting;

public class AccountDto
{
    public Guid Id { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public string TypeName => Type.ToString();
    public string? Subtype { get; set; }
    public string? Description { get; set; }
    public decimal CurrentBalance { get; set; }
    public bool IsSystemAccount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateAccountDto
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public string? Subtype { get; set; }
    public string? Description { get; set; }
    public decimal InitialBalance { get; set; } = 0;
}

public class UpdateAccountDto
{
    public string AccountName { get; set; } = string.Empty;
    public string? Subtype { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
