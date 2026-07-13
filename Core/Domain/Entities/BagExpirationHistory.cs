using Domain.Common;

namespace Domain.Entities;

public class BagExpirationHistory : BaseEntity
{
    public string? BagId { get; set; }
    public Bag? Bag { get; set; }
    public DateTime OldExpirationDate { get; set; }
    public DateTime NewExpirationDate { get; set; }
    public string? ChangedBy { get; set; }
    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
    public string? Note { get; set; }
}
