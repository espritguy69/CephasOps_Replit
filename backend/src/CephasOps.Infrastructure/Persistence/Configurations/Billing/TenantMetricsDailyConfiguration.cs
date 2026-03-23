using CephasOps.Domain.Billing.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Billing;

public class TenantMetricsDailyConfiguration : IEntityTypeConfiguration<TenantMetricsDaily>
{
    public void Configure(EntityTypeBuilder<TenantMetricsDaily> builder)
    {
        builder.ToTable("TenantMetricsDaily");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TenantId, x.DateUtc }).IsUnique();
    }
}
