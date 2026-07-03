using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class DropConfigConfiguration : BaseEntityConfiguration<DropConfig>
{
    public override void Configure(EntityTypeBuilder<DropConfig> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Titulo).HasMaxLength(NameConsts.MaxLength).IsRequired();
        builder.Property(x => x.Subtitulo).HasMaxLength(DescriptionConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.DataLiberacao).IsRequired();
        builder.Property(x => x.Ativo).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
    }
}
