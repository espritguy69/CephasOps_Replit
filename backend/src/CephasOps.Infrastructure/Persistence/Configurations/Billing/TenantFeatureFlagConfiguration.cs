using CephasOps.Domain.Billing.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Billing;

public class TenantFeatureFlagConfiguration : IEntityTypeConfiguration<TenantFeatureFlag>
{
    public void Configure(EntityTypeBuilder<TenantFeatureFlag> builder)
    {
        builder.ToTable("TenantFeatureFlags");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FeatureKey).IsRequired().HasMaxLength(128);
        builder.HasIndex(x => new { x.TenantId, x.FeatureKey }).IsUnique();
    }
}
