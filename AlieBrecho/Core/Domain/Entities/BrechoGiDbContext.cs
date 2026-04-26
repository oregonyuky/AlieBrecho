using Microsoft.EntityFrameworkCore;

namespace AlieBrecho.Core.Domain.Entities
{
    public class BrechoGiDbContext : DbContext
    {
        public BrechoGiDbContext(DbContextOptions<BrechoGiDbContext> options)
            : base(options)
        {
        }

        public DbSet<Address> Addresses { get; set; } = null!;
        public DbSet<AdminEmployee> AdminEmployees { get; set; } = null!;
        public DbSet<AdminLogin> AdminLogins { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<SubCategory> SubCategories { get; set; } = null!;
        public DbSet<Customer> Customers { get; set; } = null!;
        public DbSet<ErrorViewModel> ErrorViewModels { get; set; } = null!;
        public DbSet<GenMainSliders> GenMainSliders { get; set; } = null!;
        public DbSet<GenPromoRight> GenPromoRights { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderDetail> OrderDetails { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<PaymentType> PaymentTypes { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<RecentlyViewed> RecentlyViewed { get; set; } = null!;
        public DbSet<Review> Reviews { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<ShippingDetail> ShippingDetails { get; set; } = null!;
        public DbSet<Supplier> Suppliers { get; set; } = null!;
        public DbSet<TempShpData> TempShpDatas { get; set; } = null!;
        public DbSet<TopSoldProduct> TopSoldProducts { get; set; } = null!;
        public DbSet<UserModel> UserModels { get; set; } = null!;
        public DbSet<Wishlist> Wishlists { get; set; } = null!;
    }
}
