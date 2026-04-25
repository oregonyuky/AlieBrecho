using Microsoft.AspNetCore.Mvc.ViewEngines;
using System.ComponentModel.DataAnnotations;

namespace AlieBrecho.Core.Domain.Entities
{
    public class Customer
    {
        [Key]
        public int CustomerId { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;


        public Address address { get; set; }

        public String Cpf { get; set; } = string.Empty;

        public String? PhoneNumber { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public string Email { get; set; } = string.Empty;

        public string? AltEmail { get; set; }

        public string? Picture { get; set; }

        public string Status { get; set; } = "Active";

        public DateTime? LastLogin { get; set; }

        public DateTime Created { get; set; } = DateTime.UtcNow;

        public string? Notes { get; set; }

        public ICollection<Order> Orders { get; set; } = new List<Order>();

        public ICollection<RecentlyViewed> RecentlyViews { get; set; } = new List<RecentlyViewed>();

        public ICollection<Review> Reviews { get; set; } = new List<Review>();

        public ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
    }
}
