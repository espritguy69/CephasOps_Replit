using CephasOps.Domain.Rates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Rates;

public class GponSiCustomRateConfiguration : IEntityTypeConfiguration<GponSiCustomRate>
{
    public void Configure(EntityTypeBuilder<GponSiCustomRate> builder)
    {
        builder.ToTable("GponSiCustomRates");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.CustomPayoutAmount)
            .HasPrecision(18, 4);

        builder.Property(r => r.Currency)
            .HasMaxLength(10)
            .HasDefaultValue("MYR");

        builder.Property(r => r.Reason)
            .HasMaxLength(500);

        // Indexes for rate lookups - keyed by ServiceInstallerId + OrderType + OrderCategory + InstallationMethod
        builder.HasIndex(r => new { r.ServiceInstallerId, r.OrderTypeId, r.OrderCategoryId, r.InstallationMethodId });
        builder.HasIndex(r => new { r.ServiceInstallerId, r.PartnerGroupId });
        builder.HasIndex(r => new { r.CompanyId, r.IsActive });
        builder.HasIndex(r => new { r.ValidFrom, r.ValidTo });
        builder.HasIndex(r => r.ApprovedByUserId);
    }
}

