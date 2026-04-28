using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class FileImageConfiguration : BaseEntityConfiguration<FileImage>
{
    public override void Configure(EntityTypeBuilder<FileImage> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Name).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Description).HasMaxLength(DescriptionConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.OriginalName).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.GeneratedName).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Extension).HasMaxLength(CodeConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.FileSize).IsRequired(false);
    }
}