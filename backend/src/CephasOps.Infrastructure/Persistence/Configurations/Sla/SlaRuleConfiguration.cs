using CephasOps.Domain.Sla.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Sla;

public class SlaRuleConfiguration : IEntityTypeConfiguration<SlaRule>
{
    public void Configure(EntityTypeBuilder<SlaRule> builder)
    {
        builder.ToTable("SlaRules");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.RuleType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.TargetType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.TargetName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.Enabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(r => r.CompanyId);
        builder.HasIndex(r => new { r.CompanyId, r.Enabled, r.RuleType });
        builder.HasIndex(r => new { r.CompanyId, r.TargetType, r.TargetName });
    }
}
