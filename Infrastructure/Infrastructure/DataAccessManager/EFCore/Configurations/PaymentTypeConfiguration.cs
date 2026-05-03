using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class PaymentTypeConfiguration : BaseEntityConfiguration<PaymentType>
{
    public override void Configure(EntityTypeBuilder<PaymentType> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.TypeName).HasMaxLength(NameConsts.MaxLength).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(DescriptionConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
    }
}
