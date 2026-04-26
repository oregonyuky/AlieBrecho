using System.ComponentModel.DataAnnotations;

namespace AlieBrecho.Core.Domain.Entities
{
    public class OrderDetail : BaseEntity
    {
        public int OrderId { get; set; }

        public int ProductId { get; set; }

        public decimal? UnitPrice { get; set; }

        public int? Quantity { get; set; }

        public decimal? Discount { get; set; }

        public decimal? TotalAmount { get; set; }

        public DateTime? OrderDate { get; set; }

        // 🔗 Relacionamentos
        public Order Order { get; set; } = null!;

        public Product Product { get; set; } = null!;
    }
}
