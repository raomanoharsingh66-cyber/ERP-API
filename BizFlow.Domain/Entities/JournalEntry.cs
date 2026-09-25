using BizFlow.Domain.Common;
using BizFlow.Domain.Enums;

namespace BizFlow.Domain.Entities;

public class JournalEntry : BaseEntity, IAuditableEntity, IBusinessScoped
{
    public Guid BusinessId { get; set; }
    public Business? Business { get; set; }

    public string EntryNumber { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public string? Reference { get; set; }
    public string Narration { get; set; } = string.Empty;
    public JournalEntryType EntryType { get; set; } = JournalEntryType.Manual;

    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public bool IsPosted { get; set; } = true;

    public ICollection<JournalEntryLine> Lines { get; set; } = new List<JournalEntryLine>();
}
