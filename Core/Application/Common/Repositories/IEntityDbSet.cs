using Microsoft.EntityFrameworkCore;
using Domain.Entities;

namespace Application.Common.Repositories;

public interface IEntityDbSet
{
    public DbSet<Category> Category { get; set; }
}