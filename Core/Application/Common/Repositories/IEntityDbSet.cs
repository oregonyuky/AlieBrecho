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

}
