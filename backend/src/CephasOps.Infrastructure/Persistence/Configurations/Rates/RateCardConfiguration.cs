using CephasOps.Domain.Rates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Rates;

public class RateCardConfiguration : IEntityTypeConfiguration<RateCard>
{
    public void Configure(EntityTypeBuilder<RateCard> builder)
    {
        builder.ToTable("RateCards");

        builder.HasKey(rc => rc.Id);

        builder.Property(rc => rc.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(rc => rc.Description)
            .HasMaxLength(1000);

        builder.Property(rc => rc.RateContext)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(rc => rc.RateKind)
            .HasConversion<string>()
            .HasMaxLength(50);

        // Indexes
        builder.HasIndex(rc => new { rc.CompanyId, rc.RateContext, rc.RateKind, rc.IsActive });
        builder.HasIndex(rc => new { rc.CompanyId, rc.VerticalId, rc.DepartmentId });
        builder.HasIndex(rc => new { rc.ValidFrom, rc.ValidTo });

        // Relationships
        builder.HasMany(rc => rc.Lines)
            .WithOne(rcl => rcl.RateCard)
            .HasForeignKey(rcl => rcl.RateCardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

