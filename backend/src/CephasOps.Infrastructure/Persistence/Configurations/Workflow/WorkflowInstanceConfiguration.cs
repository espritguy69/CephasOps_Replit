using CephasOps.Domain.Workflow.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Workflow;

public class WorkflowInstanceConfiguration : IEntityTypeConfiguration<WorkflowInstance>
{
    public void Configure(EntityTypeBuilder<WorkflowInstance> builder)
    {
        builder.ToTable("WorkflowInstances");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.WorkflowType).IsRequired().HasMaxLength(128);
        builder.Property(e => e.EntityType).IsRequired().HasMaxLength(128);
        builder.Property(e => e.CurrentStep).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(32);
        builder.Property(e => e.CorrelationId).HasMaxLength(128);
        builder.Property(e => e.PayloadJson).HasMaxLength(8000);
        builder.HasIndex(e => new { e.WorkflowType, e.EntityType, e.EntityId });
        builder.HasIndex(e => e.CorrelationId).HasFilter("\"CorrelationId\" IS NOT NULL");
    }
}
