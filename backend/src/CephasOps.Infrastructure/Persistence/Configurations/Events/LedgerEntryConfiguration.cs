using CephasOps.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Events;

public class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.ToTable("LedgerEntries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.LedgerFamily).IsRequired().HasMaxLength(64);
        builder.Property(e => e.Category).HasMaxLength(128);
        builder.Property(e => e.EntityType).HasMaxLength(128);
        builder.Property(e => e.EventType).IsRequired().HasMaxLength(128);
        builder.Property(e => e.OrderingStrategyId).HasMaxLength(64);
        builder.Property(e => e.CorrelationId).HasMaxLength(128);

        builder.HasIndex(e => new { e.CompanyId, e.LedgerFamily, e.OccurredAtUtc });
        builder.HasIndex(e => new { e.EntityType, e.EntityId, e.LedgerFamily });
        builder.HasIndex(e => e.RecordedAtUtc);
        builder.HasIndex(e => e.SourceEventId).HasFilter("\"SourceEventId\" IS NOT NULL");
        builder.HasIndex(e => e.ReplayOperationId).HasFilter("\"ReplayOperationId\" IS NOT NULL");

        // Idempotency: one entry per (SourceEventId, LedgerFamily) when event-driven
        builder.HasIndex(e => new { e.SourceEventId, e.LedgerFamily })
            .IsUnique()
            .HasFilter("\"SourceEventId\" IS NOT NULL");

        // Idempotency: one entry per (ReplayOperationId, LedgerFamily) when operation-driven
        builder.HasIndex(e => new { e.ReplayOperationId, e.LedgerFamily })
            .IsUnique()
            .HasFilter("\"ReplayOperationId\" IS NOT NULL");
    }
}
