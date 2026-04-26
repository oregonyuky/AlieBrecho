using System.ComponentModel.DataAnnotations;

namespace AlieBrecho.Core.Domain.Entities
{
    public class RecentlyViewed : BaseEntity
    {
        public int CustomerId { get; set; }

        public int ProductId { get; set; }

        public DateTime ViewDate { get; set; } = DateTime.UtcNow;

        public string? Note { get; set; }

        // 🔗 Relacionamentos
        public Customer Customer { get; set; } = null!;

        public Product Product { get; set; } = null!;
    }
}
