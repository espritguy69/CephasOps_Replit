using CephasOps.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Events;

public class ReplayOperationConfiguration : IEntityTypeConfiguration<ReplayOperation>
{
    public void Configure(EntityTypeBuilder<ReplayOperation> builder)
    {
        builder.ToTable("ReplayOperations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EventType).HasMaxLength(200);
        builder.Property(e => e.Status).HasMaxLength(50);
        builder.Property(e => e.EntityType).HasMaxLength(200);
        builder.Property(e => e.CorrelationId).HasMaxLength(200);
        builder.Property(e => e.ReplayCorrelationId).HasMaxLength(200);
        builder.Property(e => e.ReplayReason).HasMaxLength(2000);
        builder.Property(e => e.Notes).HasMaxLength(4000);
        builder.Property(e => e.State).HasMaxLength(50);
        builder.Property(e => e.ReplayTarget).HasMaxLength(50);
        builder.Property(e => e.ReplayMode).HasMaxLength(50);
        builder.Property(e => e.ErrorSummary).HasMaxLength(2000);
        builder.Property(e => e.OrderingStrategyId).HasMaxLength(100);
        builder.Property(e => e.RerunReason).HasMaxLength(500);

        builder.Property(e => e.CancelRequestedAtUtc);

        builder.HasIndex(e => e.RequestedAtUtc);
        builder.HasIndex(e => new { e.CompanyId, e.RequestedAtUtc });
        builder.HasIndex(e => new { e.CompanyId, e.State, e.RequestedAtUtc });
        builder.HasIndex(e => e.RequestedByUserId);
        builder.HasIndex(e => e.RetriedFromOperationId);
        builder.HasIndex(e => e.WorkerId);
    }
}
