using BizFlow.Domain.Common;

namespace BizFlow.Domain.Entities;

public class UnitOfMeasure : BaseEntity, IBusinessScoped
{
    public Guid BusinessId { get; set; }
    public string Name { get; set; } = string.Empty; // e.g. "Piece", "Kilogram", "Meter"
    public string Code { get; set; } = string.Empty; // e.g. "PCS", "KG", "MTR"
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Business Business { get; set; } = null!;
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
