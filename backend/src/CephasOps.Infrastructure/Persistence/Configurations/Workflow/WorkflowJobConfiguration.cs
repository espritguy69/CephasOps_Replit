using CephasOps.Domain.Workflow.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Workflow;

public class WorkflowJobConfiguration : IEntityTypeConfiguration<WorkflowJob>
{
    public void Configure(EntityTypeBuilder<WorkflowJob> builder)
    {
        builder.ToTable("WorkflowJobs");

        builder.HasKey(wj => wj.Id);

        builder.Property(wj => wj.EntityType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(wj => wj.CurrentStatus)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(wj => wj.TargetStatus)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(wj => wj.State)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(wj => wj.LastError)
            .HasMaxLength(2000);

        builder.Property(wj => wj.PayloadJson)
            .HasColumnType("jsonb");

        builder.Property(wj => wj.CorrelationId)
            .HasMaxLength(100);

        builder.HasOne(wj => wj.WorkflowDefinition)
            .WithMany()
            .HasForeignKey(wj => wj.WorkflowDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(wj => new { wj.CompanyId, wj.EntityType, wj.EntityId });
        builder.HasIndex(wj => new { wj.CompanyId, wj.State, wj.CreatedAt });
        builder.HasIndex(wj => new { wj.WorkflowDefinitionId, wj.EntityId });
    }
}

