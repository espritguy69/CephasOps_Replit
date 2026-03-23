using CephasOps.Domain.Sla.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Sla;

public class SlaBreachConfiguration : IEntityTypeConfiguration<SlaBreach>
{
    public void Configure(EntityTypeBuilder<SlaBreach> builder)
    {
        builder.ToTable("SlaBreaches");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.TargetType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(b => b.TargetId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(b => b.CorrelationId)
            .HasMaxLength(100);

        builder.Property(b => b.Severity)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(b => b.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(b => b.Title)
            .HasMaxLength(500);

        builder.HasIndex(b => b.CompanyId);
        builder.HasIndex(b => b.RuleId);
        builder.HasIndex(b => new { b.CompanyId, b.Status, b.DetectedAtUtc });
        builder.HasIndex(b => new { b.CompanyId, b.Severity });
        builder.HasIndex(b => b.CorrelationId);
    }
}
