using CephasOps.Domain.Billing.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Billing;

public class BillingRatecardConfiguration : IEntityTypeConfiguration<BillingRatecard>
{
    public void Configure(EntityTypeBuilder<BillingRatecard> builder)
    {
        builder.ToTable("BillingRatecards");

        builder.HasKey(br => br.Id);

        builder.Property(br => br.BuildingType)
            .HasMaxLength(100);

        builder.Property(br => br.Description)
            .HasMaxLength(500);

        builder.Property(br => br.Amount)
            .HasPrecision(18, 2);

        builder.Property(br => br.TaxRate)
            .HasPrecision(5, 4);

        builder.Property(br => br.ServiceCategory)
            .HasMaxLength(50);

        // Indexes for rate lookups
        // Per PARTNER_MODULE.md: Rate lookup uses partnerGroupId first, then partnerId override
        builder.HasIndex(br => new { br.CompanyId, br.PartnerGroupId, br.PartnerId, br.OrderTypeId });
        builder.HasIndex(br => new { br.CompanyId, br.PartnerId, br.OrderTypeId });
        builder.HasIndex(br => new { br.CompanyId, br.PartnerGroupId, br.OrderTypeId, br.InstallationMethodId });
        builder.HasIndex(br => new { br.CompanyId, br.IsActive });
        builder.HasIndex(br => new { br.CompanyId, br.EffectiveFrom, br.EffectiveTo });
    }
}

