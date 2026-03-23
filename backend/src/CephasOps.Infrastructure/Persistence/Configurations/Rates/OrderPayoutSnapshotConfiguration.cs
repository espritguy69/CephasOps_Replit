using CephasOps.Domain.Rates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Rates;

public class OrderPayoutSnapshotConfiguration : IEntityTypeConfiguration<OrderPayoutSnapshot>
{
    public void Configure(EntityTypeBuilder<OrderPayoutSnapshot> builder)
    {
        builder.ToTable("OrderPayoutSnapshots");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BaseAmount).HasPrecision(18, 4);
        builder.Property(x => x.FinalPayout).HasPrecision(18, 4);
        builder.Property(x => x.Currency).HasMaxLength(3);
        builder.Property(x => x.ResolutionMatchLevel).HasMaxLength(64);
        builder.Property(x => x.PayoutPath).HasMaxLength(64);
        builder.Property(x => x.Provenance).HasMaxLength(32);
        builder.Property(x => x.ModifierTraceJson).HasColumnType("text");
        builder.Property(x => x.ResolutionTraceJson).HasColumnType("text");

        builder.HasIndex(x => x.OrderId).IsUnique();
    }
}
