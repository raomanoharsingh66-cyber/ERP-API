namespace BizFlow.Domain.Common;

public interface IAuditableEntity
{
    DateTimeOffset CreatedOn { get; set; }
    string? CreatedBy { get; set; }
    DateTimeOffset? UpdatedOn { get; set; }
    string? UpdatedBy { get; set; }
}
