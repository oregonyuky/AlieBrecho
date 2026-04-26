using Microsoft.AspNetCore.Mvc.ViewEngines;
using System.ComponentModel.DataAnnotations;

namespace AlieBrecho.Core.Domain.Entities
{
    public class Product : BaseEntity
    {
        public string Name { get; set; } = string.Empty;

        public int CategoryId { get; set; }

        public int? SubCategoryId { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal? OldPrice { get; set; }

        public string? UnitWeight { get; set; }

        public string? Size { get; set; }

        public decimal? Discount { get; set; }

        public int? UnitsOnOrder { get; set; }

        public bool ProductAvailable { get; set; } = true;

        public string? ImageUrl { get; set; }

        public string? AltText { get; set; }

        public bool AddBadge { get; set; } = false;

        public string? OfferTitle { get; set; }

        public string? OfferBadgeClass { get; set; }

        public string? ShortDescription { get; set; }

        public string? LongDescription { get; set; }

        public string? Picture1 { get; set; }

        public string? Picture2 { get; set; }

        public string? Picture3 { get; set; }

        public string? Picture4 { get; set; }

        public string? Picture5 { get; set; }

        public string? Note { get; set; }

        // 🔗 Relacionamentos
        public Category Category { get; set; } = null!;

        public SubCategory? SubCategory { get; set; }

        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

        public ICollection<RecentlyViewed> RecentlyViews { get; set; } = new List<RecentlyViewed>();

        public ICollection<Review> Reviews { get; set; } = new List<Review>();

        public ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
    }
}
