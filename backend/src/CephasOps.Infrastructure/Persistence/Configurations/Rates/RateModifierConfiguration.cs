using CephasOps.Domain.Rates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Rates;

public class RateModifierConfiguration : IEntityTypeConfiguration<RateModifier>
{
    public void Configure(EntityTypeBuilder<RateModifier> builder)
    {
        builder.ToTable("RateModifiers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AdjustmentValue).HasPrecision(18, 4);
        builder.Property(x => x.ModifierValueString).HasMaxLength(100);
        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.HasIndex(x => new { x.CompanyId, x.ModifierType, x.IsActive })
            .HasFilter("\"IsDeleted\" = false");
    }
}
