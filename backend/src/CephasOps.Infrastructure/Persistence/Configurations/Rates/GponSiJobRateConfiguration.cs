using CephasOps.Domain.Rates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Rates;

public class GponSiJobRateConfiguration : IEntityTypeConfiguration<GponSiJobRate>
{
    public void Configure(EntityTypeBuilder<GponSiJobRate> builder)
    {
        builder.ToTable("GponSiJobRates");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.SiLevel)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.PayoutAmount)
            .HasPrecision(18, 4);

        builder.Property(r => r.Currency)
            .HasMaxLength(10)
            .HasDefaultValue("MYR");

        builder.Property(r => r.Notes)
            .HasMaxLength(500);

        // Indexes for rate lookups - keyed by OrderType + OrderCategory + InstallationMethod + SiLevel
        builder.HasIndex(r => new { r.OrderTypeId, r.OrderCategoryId, r.InstallationMethodId, r.SiLevel });
        builder.HasIndex(r => new { r.PartnerGroupId, r.OrderTypeId, r.OrderCategoryId });
        builder.HasIndex(r => new { r.CompanyId, r.IsActive });
        builder.HasIndex(r => new { r.ValidFrom, r.ValidTo });
    }
}

