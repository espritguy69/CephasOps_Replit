using CephasOps.Domain.Settings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Settings;

public class EscalationRuleConfiguration : IEntityTypeConfiguration<EscalationRule>
{
    public void Configure(EntityTypeBuilder<EscalationRule> builder)
    {
        builder.ToTable("escalation_rules");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .HasMaxLength(1000);

        builder.Property(e => e.EntityType)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Order");

        builder.Property(e => e.OrderType)
            .HasMaxLength(100);

        builder.Property(e => e.TriggerType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.TriggerStatus)
            .HasMaxLength(50);

        builder.Property(e => e.TriggerConditionsJson)
            .HasMaxLength(4000);

        builder.Property(e => e.EscalationType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.TargetRole)
            .HasMaxLength(100);

        builder.Property(e => e.TargetStatus)
            .HasMaxLength(50);

        builder.Property(e => e.EscalationMessage)
            .HasMaxLength(1000);

        builder.Property(e => e.ContinueEscalation)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.Priority)
            .IsRequired()
            .HasDefaultValue(100);

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.StopOnMatch)
            .IsRequired()
            .HasDefaultValue(false);

        // Indexes
        builder.HasIndex(e => e.CompanyId);
        builder.HasIndex(e => e.EntityType);
        builder.HasIndex(e => e.TriggerType);
        builder.HasIndex(e => e.EscalationType);
        builder.HasIndex(e => e.Priority);
        builder.HasIndex(e => new { e.CompanyId, e.EntityType, e.TriggerType, e.Priority });
        builder.HasIndex(e => new { e.CompanyId, e.IsActive, e.EffectiveFrom, e.EffectiveTo });
    }
}

