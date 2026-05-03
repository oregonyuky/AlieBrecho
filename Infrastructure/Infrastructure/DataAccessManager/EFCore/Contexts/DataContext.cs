using Application.Common.Repositories;
using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Configurations;
using Infrastructure.SecurityManager.AspNetIdentity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataAccessManager.EFCore.Contexts;

public class DataContext : IdentityDbContext<ApplicationUser>, IEntityDbSet
{
    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
    }
    public DbSet<FileImage> FileImage { get; set; }
    public DbSet<FileDocument> FileDocument { get; set; }
    public DbSet<Token> Token { get; set; }
    public DbSet<Company> Company { get; set; }
    public DbSet<Customer> Customer { get; set; }

    public DbSet<Category> Category { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new CategoryConfiguration());
        modelBuilder.ApplyConfiguration(new FileImageConfiguration());
        modelBuilder.ApplyConfiguration(new FileDocumentConfiguration());
        modelBuilder.ApplyConfiguration(new TokenConfiguration());
        modelBuilder.ApplyConfiguration(new CompanyConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerConfiguration());
    }
}
