using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public sealed class ContactMessageConfiguration : BaseEntityConfiguration<ContactMessage>
{
    public override void Configure(EntityTypeBuilder<ContactMessage> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(180).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Subject).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.IsRead).HasDefaultValue(false).IsRequired();
        builder.Property(x => x.ReceivedAtUtc).IsRequired();
        builder.Property(x => x.ReadAtUtc).IsRequired(false);
        builder.HasIndex(x => new { x.IsRead, x.ReceivedAtUtc });
    }
}
