using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class BagItemConfiguration : IEntityTypeConfiguration<BagItem>
{
    public void Configure(EntityTypeBuilder<BagItem> builder)
    {
        builder.Property(x => x.Price).HasColumnType("decimal(10,2)");
        builder.Property(x => x.Weight).HasColumnType("decimal(10,3)");
        builder.HasIndex(x => x.ProductId)
            .IsUnique()
            .HasDatabaseName("UX_BagItem_ActiveReservation_ProductId");
    }
}
