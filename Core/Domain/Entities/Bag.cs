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

    public string? CurrentPaymentId { get; set; }
    public string? CurrentPaymentProvider { get; set; }
    public string? CurrentPaymentQrCodeBase64 { get; set; }
    public string? CurrentPaymentQrCode { get; set; }
    public DateTime? CurrentPaymentExpiresAt { get; set; }

    public ICollection<BagItem>? Items { get; set; }
    public ICollection<BagExpirationHistory>? ExpirationHistory { get; set; }
}
