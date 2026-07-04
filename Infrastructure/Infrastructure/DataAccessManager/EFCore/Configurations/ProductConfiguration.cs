using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class ProductConfiguration : BaseEntityConfiguration<Product>
{
    public override void Configure(EntityTypeBuilder<Product> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Name).HasMaxLength(NameConsts.MaxLength).IsRequired();
        builder.Property(x => x.MainImageURL).HasMaxLength(PathConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.AltText).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.ShortDescription).HasMaxLength(DescriptionConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.LongDescription).HasMaxLength(DescriptionConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Picture1).HasMaxLength(PathConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Picture2).HasMaxLength(PathConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Picture3).HasMaxLength(PathConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Picture4).HasMaxLength(PathConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Note).HasMaxLength(DescriptionConsts.MaxLength).IsRequired(false);

        builder.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryID)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.DropConfig)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.DropConfigId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Sizes)
            .WithOne(x => x.Product)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
