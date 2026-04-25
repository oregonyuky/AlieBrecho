using System.ComponentModel.DataAnnotations;

namespace AlieBrecho.Core.Domain.Entities
{
    public class Wishlist
    {
        [Key]
        public int WishlistId { get; set; }

        public int CustomerId { get; set; }

        public int ProductId { get; set; }

        public bool IsActive { get; set; } = true;

        // 🔗 Relacionamentos
        public Customer Customer { get; set; } = null!;

        public Product Product { get; set; } = null!;
    }
}
