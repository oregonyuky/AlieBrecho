using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class BagExpirationHistoryConfiguration : BaseEntityConfiguration<BagExpirationHistory>
{
    public override void Configure(EntityTypeBuilder<BagExpirationHistory> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.BagId).HasMaxLength(50).IsRequired();
        builder.Property(x => x.OldExpirationDate).IsRequired();
        builder.Property(x => x.NewExpirationDate).IsRequired();
        builder.Property(x => x.ChangedBy).HasMaxLength(255).IsRequired(false);
        builder.Property(x => x.ChangedAtUtc).IsRequired();
        builder.Property(x => x.Note).HasMaxLength(1000).IsRequired(false);

        builder.HasOne(x => x.Bag)
            .WithMany(x => x.ExpirationHistory)
            .HasForeignKey(x => x.BagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
