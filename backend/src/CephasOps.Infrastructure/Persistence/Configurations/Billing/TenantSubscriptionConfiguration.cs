using CephasOps.Domain.Billing.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Billing;

public class TenantSubscriptionConfiguration : IEntityTypeConfiguration<TenantSubscription>
{
    public void Configure(EntityTypeBuilder<TenantSubscription> builder)
    {
        builder.ToTable("TenantSubscriptions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.ExternalSubscriptionId).HasMaxLength(256);
        builder.HasIndex(s => s.TenantId);
        builder.HasIndex(s => s.BillingPlanId);
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.ExternalSubscriptionId).HasFilter("\"ExternalSubscriptionId\" IS NOT NULL");
    }
}
