using CephasOps.Domain.Settings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Settings;

public class KpiProfileConfiguration : IEntityTypeConfiguration<KpiProfile>
{
    public void Configure(EntityTypeBuilder<KpiProfile> builder)
    {
        builder.ToTable("KpiProfiles");

        builder.HasKey(k => k.Id);

        builder.Property(k => k.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(k => k.OrderType)
            .IsRequired()
            .HasMaxLength(100);

#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
        builder.HasIndex(k => new { k.CompanyId, k.PartnerId, k.OrderType, k.BuildingTypeId, k.EffectiveFrom });
#pragma warning restore CS0618
        builder.HasIndex(k => new { k.CompanyId, k.IsDefault, k.OrderType });
        // Note: IsActive can be determined by EffectiveFrom/EffectiveTo date range, no separate property needed
    }
}

