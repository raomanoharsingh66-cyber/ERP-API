using BizFlow.Domain.Enums;

namespace BizFlow.Application.DTOs.Accounting;

public class JournalEntryLineDto
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Description { get; set; }
}

public class JournalEntryDto
{
    public Guid Id { get; set; }
    public string EntryNumber { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public string? Reference { get; set; }
    public string Narration { get; set; } = string.Empty;
    public JournalEntryType EntryType { get; set; }
    public string EntryTypeName => EntryType.ToString();
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public bool IsPosted { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<JournalEntryLineDto> Lines { get; set; } = new();
}

public class CreateJournalEntryLineDto
{
    public Guid AccountId { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Description { get; set; }
}

public class CreateJournalEntryDto
{
    public DateTime EntryDate { get; set; } = DateTime.UtcNow;
    public string? Reference { get; set; }
    public string Narration { get; set; } = string.Empty;
    public JournalEntryType EntryType { get; set; } = JournalEntryType.Manual;
    public List<CreateJournalEntryLineDto> Lines { get; set; } = new();
}

public class JournalEntryFilterDto
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public Guid? AccountId { get; set; }
    public JournalEntryType? EntryType { get; set; }
    public string? Search { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
