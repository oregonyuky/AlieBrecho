using System.ComponentModel.DataAnnotations;

namespace AlieBrecho.Core.Domain.Entities
{
    public class Category : BaseEntity
    {
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // 🔗 Relacionamentos
        public ICollection<SubCategory> SubCategories { get; set; } = new List<SubCategory>();

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
