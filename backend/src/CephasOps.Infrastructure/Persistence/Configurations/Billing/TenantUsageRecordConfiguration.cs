using CephasOps.Domain.Billing.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Billing;

public class TenantUsageRecordConfiguration : IEntityTypeConfiguration<TenantUsageRecord>
{
    public void Configure(EntityTypeBuilder<TenantUsageRecord> builder)
    {
        builder.ToTable("TenantUsageRecords");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.MetricKey).IsRequired().HasMaxLength(64);
        builder.Property(r => r.Quantity).HasPrecision(18, 4);
        builder.Property(r => r.UpdatedAtUtc);
        builder.HasIndex(r => new { r.TenantId, r.MetricKey, r.PeriodStartUtc });
    }
}
