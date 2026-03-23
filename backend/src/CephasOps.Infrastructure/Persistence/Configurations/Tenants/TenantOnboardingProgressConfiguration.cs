using CephasOps.Domain.Tenants.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Tenants;

public class TenantOnboardingProgressConfiguration : IEntityTypeConfiguration<TenantOnboardingProgress>
{
    public void Configure(EntityTypeBuilder<TenantOnboardingProgress> builder)
    {
        builder.ToTable("TenantOnboardingProgress");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TenantId).IsUnique();
    }
}
