using Domain.Common;

namespace Domain.Entities;

public class PackageCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int CapacityPoints { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<ShippingBox> ShippingBoxes { get; set; } = new List<ShippingBox>();
}
