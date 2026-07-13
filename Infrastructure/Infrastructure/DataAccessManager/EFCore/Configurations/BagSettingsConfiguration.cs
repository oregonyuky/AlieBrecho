using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class BagSettingsConfiguration : BaseEntityConfiguration<BagSettings>
{
    public override void Configure(EntityTypeBuilder<BagSettings> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.DefaultDurationUnit).HasMaxLength(20).IsRequired();
        builder.Property(x => x.ExtensionDurationUnit).HasMaxLength(20).IsRequired();
        builder.Property(x => x.ExtensionResponseDeadlineUnit).HasMaxLength(20).IsRequired();
    }
}
