using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class TokenConfiguration : BaseEntityConfiguration<Token>
{
    public override void Configure(EntityTypeBuilder<Token> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.UserId).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.RefreshToken).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.ExpiryDate).IsRequired();
    }
}