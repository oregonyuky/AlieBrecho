using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlieBrecho.Core.Domain.Entities
{
    public class Address : BaseEntity
    {
        [Required]
        [MaxLength(150)]
        public string Street { get; set; } = string.Empty;

        public int? Number { get; set; }

        [MaxLength(100)]
        public string? Complement { get; set; }

        [Required]
        [MaxLength(100)]
        public string Neighborhood { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [MaxLength(2)]
        public string State { get; set; } = string.Empty; // SP, RJ...

        [Required]
        [MaxLength(8)]
        public string PostalCode { get; set; } = string.Empty; // CEP

        // 🔗 Relacionamento
        public int CustomerId { get; set; }

        public Customer Customer { get; set; }
    }
}