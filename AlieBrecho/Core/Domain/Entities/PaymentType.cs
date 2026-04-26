using System.ComponentModel.DataAnnotations;

namespace AlieBrecho.Core.Domain.Entities
{
    public class PaymentType : BaseEntity
    {
        public string TypeName { get; set; } = string.Empty;

        public string? Description { get; set; }

        // 🔗 Relacionamento
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
