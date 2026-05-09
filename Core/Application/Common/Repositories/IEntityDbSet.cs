using Microsoft.EntityFrameworkCore;
using Domain.Entities;

namespace Application.Common.Repositories;

public interface IEntityDbSet
{
    public DbSet<Token> Token { get; set; }
    public DbSet<Company> Company { get; set; }
    public DbSet<FileImage> FileImage { get; set; }
    public DbSet<FileDocument> FileDocument { get; set; }

    public DbSet<Category> Category { get; set; }
    public DbSet<Customer> Customer { get; set; }
    public DbSet<Product> Product { get; set; }
    public DbSet<ProductSize> ProductSize { get; set; }
    public DbSet<PaymentType> PaymentType { get; set; }
    public DbSet<Order> Order { get; set; }
    public DbSet<Payment> Payment { get; set; }
    public DbSet<PaymentDetail> PaymentDetail { get; set; }
    public DbSet<ShippingDetail> ShippingDetail { get; set; }
    public DbSet<OrderDetail> OrderDetail { get; set; }
    public DbSet<Bag> Bag { get; set; }
    public DbSet<BagItem> BagItem { get; set; }
    public DbSet<ShippingBox> ShippingBox { get; set; }

}
