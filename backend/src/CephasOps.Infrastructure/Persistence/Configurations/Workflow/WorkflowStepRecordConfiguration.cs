using CephasOps.Domain.Workflow.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Workflow;

public class WorkflowStepRecordConfiguration : IEntityTypeConfiguration<WorkflowStepRecord>
{
    public void Configure(EntityTypeBuilder<WorkflowStepRecord> builder)
    {
        builder.ToTable("WorkflowSteps");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.StepName).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(32);
        builder.Property(e => e.PayloadJson).HasMaxLength(8000);
        builder.Property(e => e.CompensationDataJson).HasMaxLength(8000);
        builder.HasOne(e => e.WorkflowInstance)
            .WithMany()
            .HasForeignKey(e => e.WorkflowInstanceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => e.WorkflowInstanceId);
    }
}
