using System.ComponentModel.DataAnnotations;

namespace AlieBrecho.Core.Domain.Entities
{
    public class Review : BaseEntity
    {
        public int CustomerId { get; set; }

        public int ProductId { get; set; }

        public int Rating { get; set; }

        public string? Title { get; set; }

        public string? Comment { get; set; }

        public DateTime ReviewDate { get; set; } = DateTime.UtcNow;

        public bool IsPublished { get; set; } = true;

        // 🔗 Relacionamentos
        public Customer Customer { get; set; } = null!;

        public Product Product { get; set; } = null!;
    }
}
