using CephasOps.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Events;

public class EventProcessingLogConfiguration : IEntityTypeConfiguration<EventProcessingLog>
{
    public void Configure(EntityTypeBuilder<EventProcessingLog> builder)
    {
        builder.ToTable("EventProcessingLog");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.HandlerName).IsRequired().HasMaxLength(256);
        builder.Property(e => e.State).IsRequired().HasMaxLength(32);
        builder.Property(e => e.Error).HasMaxLength(2000);
        builder.Property(e => e.CorrelationId).HasMaxLength(128);

        // One row per (EventId, HandlerName); enforces at-most-one completion per handler per event
        builder.HasIndex(e => new { e.EventId, e.HandlerName })
            .IsUnique();

        builder.HasIndex(e => e.EventId);
        builder.HasIndex(e => new { e.State, e.StartedAtUtc });
        builder.HasIndex(e => e.ReplayOperationId).HasFilter("\"ReplayOperationId\" IS NOT NULL");
    }
}
