using System.ComponentModel.DataAnnotations;

namespace AlieBrecho.Core.Domain.Entities
{
    public class TempShpData : BaseEntity
    {
        public string? SessionId { get; set; }

        public int? CustomerId { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Address { get; set; }

        public string? City { get; set; }

        public string? Province { get; set; }

        public string? PostCode { get; set; }

        public string? Country { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsCompleted { get; set; } = false;

        // 🔗 Relacionamento
        public Customer? Customer { get; set; }
    }
}
