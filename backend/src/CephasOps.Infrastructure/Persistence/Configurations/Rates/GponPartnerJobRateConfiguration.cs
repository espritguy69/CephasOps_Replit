using CephasOps.Domain.Rates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Rates;

public class GponPartnerJobRateConfiguration : IEntityTypeConfiguration<GponPartnerJobRate>
{
    public void Configure(EntityTypeBuilder<GponPartnerJobRate> builder)
    {
        builder.ToTable("GponPartnerJobRates");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.RevenueAmount)
            .HasPrecision(18, 4);

        builder.Property(r => r.Currency)
            .HasMaxLength(10)
            .HasDefaultValue("MYR");

        builder.Property(r => r.Notes)
            .HasMaxLength(500);

        // Indexes for rate lookups - keyed by OrderType + OrderCategory + InstallationMethod + PartnerGroup
        builder.HasIndex(r => new { r.PartnerGroupId, r.OrderTypeId, r.OrderCategoryId, r.InstallationMethodId });
        builder.HasIndex(r => new { r.PartnerId, r.OrderTypeId, r.OrderCategoryId, r.InstallationMethodId });
        builder.HasIndex(r => new { r.CompanyId, r.IsActive });
        builder.HasIndex(r => new { r.ValidFrom, r.ValidTo });
    }
}

