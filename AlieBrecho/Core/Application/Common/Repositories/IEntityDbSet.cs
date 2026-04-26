using Microsoft.EntityFrameworkCore;
using Domain.Entities;

namespace Application.Common.Repositories;

public interface IEntityDbSet
{
    public DbSet<Customer> Customer { get; set; }
    public DbSet<ErrorViewModel> ErrorViewModel { get; set; }
    public DbSet<GenMainSliders> GenMainSliders { get; set; }
    public DbSet<GenPromoRight> GenPromoRight { get; set; }
    public DbSet<Order> Order { get; set; }
    public DbSet<OrderDetail> OrderDetail { get; set; }
    public DbSet<Payment> Payment { get; set; }
    public DbSet<PaymentType> PaymentType { get; set; }
    public DbSet<Product> Product { get; set; }
    public DbSet<RecentlyViewed> RecentlyViewed { get; set; }
    public DbSet<Review> Review { get; set; }
    public DbSet<Role> Role { get; set; }
    public DbSet<ShippingDetail> ShippingDetail { get; set; }
    public DbSet<SubCategory> SubCategory { get; set; }
    public DbSet<Supplier> Supplier { get; set; }
    public DbSet<TempShpData> TempShpData { get; set; }
    public DbSet<TopSoldProduct> TopSoldProduct { get; set; }
    public DbSet<UserModel> UserModel { get; set; }
    public DbSet<Wishlist> Wishlist { get; set; }
}