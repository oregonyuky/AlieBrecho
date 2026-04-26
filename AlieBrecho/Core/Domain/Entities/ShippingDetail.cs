using System.ComponentModel.DataAnnotations;

namespace AlieBrecho.Core.Domain.Entities
{
    public class ShippingDetail : BaseEntity
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? Mobile { get; set; }

        public string? Address { get; set; }

        public string? Province { get; set; }

        public string? City { get; set; }

        public string? PostCode { get; set; }

        // 🔗 Relacionamento
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
