using System.ComponentModel.DataAnnotations;

namespace AlieBrecho.Core.Domain.Entities
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        public int CustomerId { get; set; }

        public int? PaymentId { get; set; }

        public int? ShippingId { get; set; }

        public decimal? Discount { get; set; }

        public decimal? Taxes { get; set; }

        public decimal TotalAmount { get; set; }

        public bool IsCompleted { get; set; } = false;

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        public bool Dispatched { get; set; } = false;

        public DateTime? DispatchedDate { get; set; }

        public bool Shipped { get; set; } = false;

        public DateTime? ShippingDate { get; set; }

        public bool Delivered { get; set; } = false;

        public DateTime? DeliveryDate { get; set; }

        public bool IsCanceled { get; set; } = false;

        public string? Notes { get; set; }

        // 🔗 Relacionamentos
        public Customer Customer { get; set; } = null!;

        public Payment? Payment { get; set; }

        public ShippingDetail? ShippingDetail { get; set; }

        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
