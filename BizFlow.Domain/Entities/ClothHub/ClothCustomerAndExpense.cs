using System;
using System.Collections.Generic;
using BizFlow.Domain.Common;

namespace BizFlow.Domain.Entities.ClothHub;

public class ClothCustomer : BaseEntity, IBusinessScoped
{
    public Guid BusinessId { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Gstin { get; set; }

    public decimal CreditLimit { get; set; } = 5000;
    public decimal CurrentOutstanding { get; set; } = 0;
    public decimal TotalSpentAmount { get; set; } = 0;
    public int TotalVisitsCount { get; set; } = 0;

    public DateTime? DateOfBirth { get; set; }
    public DateTime? AnniversaryDate { get; set; }

    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ClothExpense : BaseEntity, IBusinessScoped
{
    public Guid BusinessId { get; set; }

    public string VoucherNumber { get; set; } = string.Empty; // e.g. "EXP-20261002-0001"
    public DateTime ExpenseDate { get; set; } = DateTime.UtcNow;

    // Categories: "Shop Utility / Power", "Staff Welfare / Tea & Snacks", "Tailoring & Alterations", "Packaging, Bags & Tags", "Shop Rent & Maintenance", "Miscellaneous"
    public string ExpenseCategory { get; set; } = "Staff Welfare / Tea & Snacks";

    public decimal Amount { get; set; }
    public string PaymentMode { get; set; } = "Cash"; // "Cash", "UPI", "Bank Transfer"
    public string? PaidTo { get; set; }
    public string? Notes { get; set; }

    public string? ApprovedBy { get; set; }
}
