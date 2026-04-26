using System.ComponentModel.DataAnnotations;

namespace AlieBrecho.Core.Domain.Entities
{
    public class Payment : BaseEntity
    {
        public int TypeId { get; set; }

        public decimal? CreditAmount { get; set; }

        public decimal? DebitAmount { get; set; }

        public decimal? Balance { get; set; }

        public DateTime? PaymentDateTime { get; set; }

        // 🔗 Relacionamentos
        public PaymentType PaymentType { get; set; } = null!;

        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
