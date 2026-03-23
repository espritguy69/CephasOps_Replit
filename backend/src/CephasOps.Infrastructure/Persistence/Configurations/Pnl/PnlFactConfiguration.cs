using CephasOps.Domain.Pnl.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Pnl;

public class PnlFactConfiguration : IEntityTypeConfiguration<PnlFact>
{
    public void Configure(EntityTypeBuilder<PnlFact> builder)
    {
        builder.ToTable("PnlFacts");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Period)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(f => f.Vertical)
            .HasMaxLength(50);

        builder.Property(f => f.OrderType)
            .HasMaxLength(50);

        builder.Property(f => f.RevenueAmount)
            .HasPrecision(18, 2);

        builder.Property(f => f.DirectMaterialCost)
            .HasPrecision(18, 2);

        builder.Property(f => f.DirectLabourCost)
            .HasPrecision(18, 2);

        builder.Property(f => f.IndirectCost)
            .HasPrecision(18, 2);

        builder.Property(f => f.GrossProfit)
            .HasPrecision(18, 2);

        builder.Property(f => f.NetProfit)
            .HasPrecision(18, 2);

        builder.HasOne(f => f.PnlPeriod)
            .WithMany(p => p.PnlFacts)
            .HasForeignKey(f => f.PnlPeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(f => new { f.CompanyId, f.Period });
        builder.HasIndex(f => new { f.CompanyId, f.PartnerId });
        builder.HasIndex(f => new { f.CompanyId, f.CostCentreId });
    }
}

