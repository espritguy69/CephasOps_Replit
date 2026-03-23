using CephasOps.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Events;

public class ReplayOperationEventConfiguration : IEntityTypeConfiguration<ReplayOperationEvent>
{
    public void Configure(EntityTypeBuilder<ReplayOperationEvent> builder)
    {
        builder.ToTable("ReplayOperationEvents");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ErrorMessage).HasMaxLength(2000);
        builder.Property(e => e.EventType).HasMaxLength(200);
        builder.Property(e => e.EntityType).HasMaxLength(200);
        builder.Property(e => e.SkippedReason).HasMaxLength(500);

        builder.HasIndex(e => e.ReplayOperationId);
        builder.HasIndex(e => e.EventId);
    }
}
