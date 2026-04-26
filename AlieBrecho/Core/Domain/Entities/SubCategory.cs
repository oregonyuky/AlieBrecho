using System.ComponentModel.DataAnnotations;

namespace AlieBrecho.Core.Domain.Entities
{
    public class SubCategory : BaseEntity
    {
        public int CategoryId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // 🔗 Relacionamentos
        public Category Category { get; set; } = null!;

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
