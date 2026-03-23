using CephasOps.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Events;

public class EventStoreEntryConfiguration : IEntityTypeConfiguration<EventStoreEntry>
{
    public void Configure(EntityTypeBuilder<EventStoreEntry> builder)
    {
        builder.ToTable("EventStore");

        builder.HasKey(e => e.EventId);

        builder.Property(e => e.EventType)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Payload)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.CorrelationId)
            .HasMaxLength(100);

        builder.Property(e => e.Source).HasMaxLength(200);
        builder.Property(e => e.EntityType).HasMaxLength(200);
        builder.Property(e => e.LastError).HasMaxLength(2000);
        builder.Property(e => e.LastHandler).HasMaxLength(500);
        builder.Property(e => e.PayloadVersion).HasMaxLength(20);
        builder.Property(e => e.CausationId);

        builder.Property(e => e.ProcessingStartedAtUtc);
        builder.Property(e => e.ProcessingNodeId).HasMaxLength(200);
        builder.Property(e => e.ProcessingLeaseExpiresAtUtc);
        builder.Property(e => e.LastClaimedAtUtc);
        builder.Property(e => e.LastClaimedBy).HasMaxLength(200);
        builder.Property(e => e.LastErrorType).HasMaxLength(100);

        // Phase 8: platform envelope
        builder.Property(e => e.RootEventId);
        builder.Property(e => e.PartitionKey).HasMaxLength(500);
        builder.Property(e => e.ReplayId).HasMaxLength(100);
        builder.Property(e => e.SourceService).HasMaxLength(100);
        builder.Property(e => e.SourceModule).HasMaxLength(100);
        builder.Property(e => e.CapturedAtUtc);
        builder.Property(e => e.IdempotencyKey).HasMaxLength(500);
        builder.Property(e => e.TraceId).HasMaxLength(64);
        builder.Property(e => e.SpanId).HasMaxLength(64);
        builder.Property(e => e.Priority).HasMaxLength(50);

        builder.HasIndex(e => new { e.CompanyId, e.EventType, e.OccurredAtUtc });
        builder.HasIndex(e => e.CorrelationId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.OccurredAtUtc);
        builder.HasIndex(e => new { e.Status, e.NextRetryAtUtc });
        // Phase 8: correlation tree and replay
        builder.HasIndex(e => e.RootEventId).HasFilter("\"RootEventId\" IS NOT NULL");
        builder.HasIndex(e => e.PartitionKey).HasFilter("\"PartitionKey\" IS NOT NULL");
        builder.HasIndex(e => e.ReplayId).HasFilter("\"ReplayId\" IS NOT NULL");
        builder.HasIndex(e => new { e.PartitionKey, e.CreatedAtUtc, e.EventId }).HasFilter("\"PartitionKey\" IS NOT NULL");
    }
}
