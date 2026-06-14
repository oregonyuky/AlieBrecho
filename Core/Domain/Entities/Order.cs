using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class Order : BaseEntity
{
    public string? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public string? PaymentId { get; set; }
    public Payment? Payment { get; set; }
    public string? ShippingBoxId { get; set; }
    public ShippingBox? ShippingBox { get; set; }
    public ShippingDetail? ShippingDetail { get; set; }
    public decimal? Discount { get; set; }
    public decimal? Taxes { get; set; }
    public decimal? TotalAmount { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? DispatchedDate { get; set; }
    public DateTime? ShippedDate { get; set; }
    public DateTime? DeliveredDate { get; set; }
    public DateTime? CancelledDate { get; set; }
    public string? MelhorEnvioCartId { get; set; }
    public DateTime? MelhorEnvioCartAddedAt { get; set; }
    public DateTime? MelhorEnvioCheckoutAt { get; set; }
    public DateTime? MelhorEnvioGeneratedAt { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

}
