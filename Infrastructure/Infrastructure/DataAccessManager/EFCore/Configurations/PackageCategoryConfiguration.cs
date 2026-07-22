using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class PackageCategoryConfiguration : BaseEntityConfiguration<PackageCategory>
{
    public override void Configure(EntityTypeBuilder<PackageCategory> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.CapacityPoints).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired(false);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
    }
}
