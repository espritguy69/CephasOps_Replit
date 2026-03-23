using CephasOps.Domain.Rates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Rates;

public class CustomRateConfiguration : IEntityTypeConfiguration<CustomRate>
{
    public void Configure(EntityTypeBuilder<CustomRate> builder)
    {
        builder.ToTable("CustomRates");

        builder.HasKey(cr => cr.Id);

        builder.Property(cr => cr.Dimension1)
            .HasMaxLength(100);

        builder.Property(cr => cr.Dimension2)
            .HasMaxLength(100);

        builder.Property(cr => cr.Dimension3)
            .HasMaxLength(100);

        builder.Property(cr => cr.Dimension4)
            .HasMaxLength(100);

        builder.Property(cr => cr.CustomRateAmount)
            .HasPrecision(18, 4);

        builder.Property(cr => cr.UnitOfMeasure)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(cr => cr.Currency)
            .HasMaxLength(10)
            .HasDefaultValue("MYR");

        builder.Property(cr => cr.Reason)
            .HasMaxLength(500);

        // Indexes for custom rate lookups
        builder.HasIndex(cr => new { cr.UserId, cr.Dimension1, cr.Dimension2, cr.Dimension3, cr.Dimension4 });
        builder.HasIndex(cr => new { cr.UserId, cr.DepartmentId, cr.IsActive });
        builder.HasIndex(cr => new { cr.ValidFrom, cr.ValidTo });
    }
}

