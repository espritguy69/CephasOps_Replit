using CephasOps.Domain.Workflow.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Workflow;

public class WorkflowTransitionHistoryEntryConfiguration : IEntityTypeConfiguration<WorkflowTransitionHistoryEntry>
{
    public void Configure(EntityTypeBuilder<WorkflowTransitionHistoryEntry> builder)
    {
        builder.ToTable("WorkflowTransitionHistory");

        builder.HasKey(e => e.EventId);

        builder.Property(e => e.EntityType).IsRequired().HasMaxLength(100);
        builder.Property(e => e.FromStatus).IsRequired().HasMaxLength(100);
        builder.Property(e => e.ToStatus).IsRequired().HasMaxLength(100);

        builder.HasIndex(e => new { e.CompanyId, e.EntityType, e.EntityId });
        builder.HasIndex(e => e.OccurredAtUtc);
    }
}
