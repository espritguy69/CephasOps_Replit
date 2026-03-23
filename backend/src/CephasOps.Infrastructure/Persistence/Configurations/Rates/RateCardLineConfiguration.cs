using CephasOps.Domain.Rates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Rates;

public class RateCardLineConfiguration : IEntityTypeConfiguration<RateCardLine>
{
    public void Configure(EntityTypeBuilder<RateCardLine> builder)
    {
        builder.ToTable("RateCardLines");

        builder.HasKey(rcl => rcl.Id);

        builder.Property(rcl => rcl.Dimension1)
            .HasMaxLength(100);

        builder.Property(rcl => rcl.Dimension2)
            .HasMaxLength(100);

        builder.Property(rcl => rcl.Dimension3)
            .HasMaxLength(100);

        builder.Property(rcl => rcl.Dimension4)
            .HasMaxLength(100);

        builder.Property(rcl => rcl.RateAmount)
            .HasPrecision(18, 4);

        builder.Property(rcl => rcl.UnitOfMeasure)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(rcl => rcl.Currency)
            .HasMaxLength(10)
            .HasDefaultValue("MYR");

        builder.Property(rcl => rcl.PayoutType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(rcl => rcl.ExtraJson)
            .HasColumnType("jsonb");

        // Indexes for rate lookups
        builder.HasIndex(rcl => new { rcl.RateCardId, rcl.Dimension1, rcl.Dimension2, rcl.Dimension3, rcl.Dimension4 });
        builder.HasIndex(rcl => new { rcl.RateCardId, rcl.PartnerGroupId, rcl.PartnerId });
        builder.HasIndex(rcl => new { rcl.RateCardId, rcl.IsActive });
    }
}

