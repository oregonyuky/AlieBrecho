using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class ProductSizeConfiguration : BaseEntityConfiguration<ProductSize>
{
    public override void Configure(EntityTypeBuilder<ProductSize> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.ProductId).HasMaxLength(IdConsts.MaxLength).IsRequired();
        builder.Property(x => x.Size).HasMaxLength(CodeConsts.MaxLength).IsRequired();
    }
}
