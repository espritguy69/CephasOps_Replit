using CephasOps.Domain.Settings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Settings;

public class AutomationRuleConfiguration : IEntityTypeConfiguration<AutomationRule>
{
    public void Configure(EntityTypeBuilder<AutomationRule> builder)
    {
        builder.ToTable("automation_rules");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Description)
            .HasMaxLength(1000);

        builder.Property(a => a.RuleType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.EntityType)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Order");

        builder.Property(a => a.OrderType)
            .HasMaxLength(100);

        builder.Property(a => a.TriggerType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.TriggerConditionsJson)
            .HasMaxLength(4000);

        builder.Property(a => a.TriggerStatus)
            .HasMaxLength(50);

        builder.Property(a => a.ActionType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.ActionConfigJson)
            .HasMaxLength(4000);

        builder.Property(a => a.TargetRole)
            .HasMaxLength(100);

        builder.Property(a => a.TargetStatus)
            .HasMaxLength(50);

        builder.Property(a => a.ConditionsJson)
            .HasMaxLength(4000);

        builder.Property(a => a.Priority)
            .IsRequired()
            .HasDefaultValue(100);

        builder.Property(a => a.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(a => a.StopOnMatch)
            .IsRequired()
            .HasDefaultValue(false);

        // Indexes
        builder.HasIndex(a => a.CompanyId);
        builder.HasIndex(a => a.RuleType);
        builder.HasIndex(a => a.EntityType);
        builder.HasIndex(a => a.TriggerType);
        builder.HasIndex(a => a.ActionType);
        builder.HasIndex(a => a.Priority);
        builder.HasIndex(a => new { a.CompanyId, a.RuleType, a.IsActive });
        builder.HasIndex(a => new { a.CompanyId, a.EntityType, a.TriggerType, a.Priority });
    }
}

