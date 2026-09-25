using System.ComponentModel.DataAnnotations;

namespace BizFlow.Application.DTOs.Purchases;

public class SupplierDto
{
    public Guid Id { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? GSTIN { get; set; }
    public string? PAN { get; set; }

    public string? BillingAddress { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }

    public int PaymentTermsDays { get; set; }
    public decimal OutstandingPayable { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
}

public class CreateSupplierDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? SupplierCode { get; set; }
    public string? ContactPerson { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    public string? Phone { get; set; }
    public string? GSTIN { get; set; }
    public string? PAN { get; set; }

    public string? BillingAddress { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; } = "India";

    public int PaymentTermsDays { get; set; } = 30;
    public string? Notes { get; set; }
}

public class UpdateSupplierDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? ContactPerson { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    public string? Phone { get; set; }
    public string? GSTIN { get; set; }
    public string? PAN { get; set; }

    public string? BillingAddress { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }

    public int PaymentTermsDays { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
}
