using CephasOps.Domain.Workflow.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Workflow;

public class WorkflowDefinitionConfiguration : IEntityTypeConfiguration<WorkflowDefinition>
{
    public void Configure(EntityTypeBuilder<WorkflowDefinition> builder)
    {
        builder.ToTable("WorkflowDefinitions");

        builder.HasKey(wd => wd.Id);

        builder.Property(wd => wd.Name)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(wd => wd.EntityType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(wd => wd.Description)
            .HasMaxLength(1000);

        builder.Property(wd => wd.OrderTypeCode)
            .HasMaxLength(100);

        builder.HasMany(wd => wd.Transitions)
            .WithOne(wt => wt.WorkflowDefinition)
            .HasForeignKey(wt => wt.WorkflowDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(wd => new { wd.CompanyId, wd.EntityType, wd.IsActive });
        builder.HasIndex(wd => new { wd.CompanyId, wd.Name });
    }
}

