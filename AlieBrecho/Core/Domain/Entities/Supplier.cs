using System.ComponentModel.DataAnnotations;

namespace AlieBrecho.Core.Domain.Entities
{
    public class Supplier : BaseEntity
    {
        public string Name { get; set; } = string.Empty;

        public string? ContactName { get; set; }

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Address { get; set; }

        public string? City { get; set; }

        public string? Country { get; set; }

        public string? Website { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // 🔗 Relacionamentos
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
