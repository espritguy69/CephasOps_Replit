using CephasOps.Domain.Billing.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Billing;

public class TenantMetricsMonthlyConfiguration : IEntityTypeConfiguration<TenantMetricsMonthly>
{
    public void Configure(EntityTypeBuilder<TenantMetricsMonthly> builder)
    {
        builder.ToTable("TenantMetricsMonthly");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TenantId, x.Year, x.Month }).IsUnique();
    }
}
