using System.ComponentModel.DataAnnotations;

namespace AlieBrecho.Core.Domain.Entities
{
    public class TopSoldProduct : BaseEntity
    {
        public int ProductId { get; set; }

        public int UnitsSold { get; set; }

        public decimal? TotalRevenue { get; set; }

        public int Rank { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // 🔗 Relacionamentos
        public Product Product { get; set; } = null!;
    }
}
