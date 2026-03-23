using CephasOps.Domain.Settings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Settings;

public class ApprovalWorkflowConfiguration : IEntityTypeConfiguration<ApprovalWorkflow>
{
    public void Configure(EntityTypeBuilder<ApprovalWorkflow> builder)
    {
        builder.ToTable("approval_workflows");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Description)
            .HasMaxLength(1000);

        builder.Property(a => a.WorkflowType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.EntityType)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Order");

        builder.Property(a => a.OrderType)
            .HasMaxLength(100);

        builder.Property(a => a.MinValueThreshold)
            .HasPrecision(18, 2);

        builder.Property(a => a.RequireAllSteps)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(a => a.AllowParallelApproval)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(a => a.EscalationRole)
            .HasMaxLength(100);

        builder.Property(a => a.AutoApproveOnTimeout)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(a => a.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(a => a.IsDefault)
            .IsRequired()
            .HasDefaultValue(false);

        // Indexes
        builder.HasIndex(a => a.CompanyId);
        builder.HasIndex(a => a.WorkflowType);
        builder.HasIndex(a => a.EntityType);
        builder.HasIndex(a => new { a.CompanyId, a.WorkflowType, a.IsDefault });
        builder.HasIndex(a => new { a.CompanyId, a.IsActive, a.EffectiveFrom, a.EffectiveTo });
    }
}

public class ApprovalStepConfiguration : IEntityTypeConfiguration<ApprovalStep>
{
    public void Configure(EntityTypeBuilder<ApprovalStep> builder)
    {
        builder.ToTable("approval_steps");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.StepOrder)
            .IsRequired();

        builder.Property(s => s.ApprovalType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.TargetRole)
            .HasMaxLength(100);

        builder.Property(s => s.ExternalSource)
            .HasMaxLength(100);

        builder.Property(s => s.IsRequired)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(s => s.CanSkipIfPreviousApproved)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(s => s.AutoApproveOnTimeout)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(s => s.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Relationships
        builder.HasOne(s => s.ApprovalWorkflow)
            .WithMany(w => w.Steps)
            .HasForeignKey(s => s.ApprovalWorkflowId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(s => s.ApprovalWorkflowId);
        builder.HasIndex(s => s.CompanyId);
        builder.HasIndex(s => new { s.ApprovalWorkflowId, s.StepOrder });
    }
}

