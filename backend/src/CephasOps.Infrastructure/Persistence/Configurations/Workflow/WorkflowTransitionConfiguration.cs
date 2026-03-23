using CephasOps.Domain.Workflow.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Workflow;

public class WorkflowTransitionConfiguration : IEntityTypeConfiguration<WorkflowTransition>
{
    public void Configure(EntityTypeBuilder<WorkflowTransition> builder)
    {
        builder.ToTable("WorkflowTransitions");

        builder.HasKey(wt => wt.Id);

        builder.Property(wt => wt.FromStatus)
            .HasMaxLength(100);

        builder.Property(wt => wt.ToStatus)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(wt => wt.AllowedRolesJson)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(wt => wt.GuardConditionsJson)
            .HasColumnType("jsonb");

        builder.Property(wt => wt.SideEffectsConfigJson)
            .HasColumnType("jsonb");

        builder.HasIndex(wt => new { wt.CompanyId, wt.WorkflowDefinitionId, wt.FromStatus, wt.ToStatus })
            .IsUnique();

        builder.HasIndex(wt => new { wt.CompanyId, wt.WorkflowDefinitionId, wt.IsActive });
    }
}

