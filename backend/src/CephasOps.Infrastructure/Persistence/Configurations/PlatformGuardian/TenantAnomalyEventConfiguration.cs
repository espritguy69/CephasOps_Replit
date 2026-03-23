using CephasOps.Domain.PlatformGuardian;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.PlatformGuardian;

public class TenantAnomalyEventConfiguration : IEntityTypeConfiguration<TenantAnomalyEvent>
{
    public void Configure(EntityTypeBuilder<TenantAnomalyEvent> builder)
    {
        builder.ToTable("TenantAnomalyEvents");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Kind).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Severity).IsRequired().HasMaxLength(20);
        builder.Property(e => e.Details).HasMaxLength(2000);
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.TenantId, e.OccurredAtUtc });
        builder.HasIndex(e => new { e.Severity, e.OccurredAtUtc });
    }
}
