using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class ShippingBoxConfiguration : BaseEntityConfiguration<ShippingBox>
{
    public override void Configure(EntityTypeBuilder<ShippingBox> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Width).HasColumnType("decimal(10,2)").IsRequired(false);
        builder.Property(x => x.Length).HasColumnType("decimal(10,2)").IsRequired(false);
        builder.Property(x => x.Height).HasColumnType("decimal(10,2)").IsRequired(false);
        builder.Property(x => x.Weight).HasColumnType("decimal(10,2)").IsRequired(false);
        builder.Property(x => x.InsuranceValue).HasColumnType("decimal(10,2)").IsRequired(false);
        builder.HasOne(x => x.PackageCategory)
            .WithMany(x => x.ShippingBoxes)
            .HasForeignKey(x => x.PackageCategoryId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
        builder.Property(x => x.StockQuantity).HasDefaultValue(0).IsRequired();
        builder.Property(x => x.MaxWeight).HasColumnType("decimal(10,3)").IsRequired(false);

        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
    }
}
