using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class BagConfiguration : IEntityTypeConfiguration<Bag>
{
    public void Configure(EntityTypeBuilder<Bag> builder)
    {
        builder.Property(x => x.Status)
            .HasConversion<string>();

        builder.Property(x => x.TotalItemsValue)
            .HasColumnType("decimal(10,2)");

        builder.Property(x => x.ShippingCost)
            .HasColumnType("decimal(10,2)");

        builder.Property(x => x.TotalWeight)
            .HasColumnType("decimal(10,3)");

        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(x => x.BagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
