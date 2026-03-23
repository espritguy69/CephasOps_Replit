using CephasOps.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Events;

public class EventStoreAttemptHistoryConfiguration : IEntityTypeConfiguration<EventStoreAttemptHistory>
{
    public void Configure(EntityTypeBuilder<EventStoreAttemptHistory> builder)
    {
        builder.ToTable("EventStoreAttemptHistory");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).UseIdentityByDefaultColumn();

        builder.Property(e => e.EventType).IsRequired().HasMaxLength(200);
        builder.Property(e => e.HandlerName).IsRequired().HasMaxLength(500);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(50);
        builder.Property(e => e.ProcessingNodeId).HasMaxLength(200);
        builder.Property(e => e.ErrorType).HasMaxLength(100);
        builder.Property(e => e.ErrorMessage).HasMaxLength(2000);
        builder.Property(e => e.StackTraceSummary).HasMaxLength(2000);

        builder.HasIndex(e => e.EventId);
        builder.HasIndex(e => new { e.EventId, e.AttemptNumber });
    }
}
