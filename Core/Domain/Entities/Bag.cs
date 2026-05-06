using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class Bag : BaseEntity
{
    public string? CustomerId { get; set; }

    // Status tipado (correto)
    public BagStatus Status { get; set; } = BagStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpirationDate { get; set; }

    public DateTime LastInteractionAt { get; set; } = DateTime.UtcNow;

    public DateTime? ClosedAt { get; set; }

    public decimal TotalItemsValue { get; set; } = 0m;

    public decimal? ShippingCost { get; set; }

    public decimal TotalWeight { get; set; } = 0m;

    public bool AllItemsPaid { get; set; } = false;

    public string? Notes { get; set; }

    public ICollection<BagItem>? Items { get; set; }
}
