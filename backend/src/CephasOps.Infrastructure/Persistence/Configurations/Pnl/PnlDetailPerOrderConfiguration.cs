using CephasOps.Domain.Pnl.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Pnl;

public class PnlDetailPerOrderConfiguration : IEntityTypeConfiguration<PnlDetailPerOrder>
{
    public void Configure(EntityTypeBuilder<PnlDetailPerOrder> builder)
    {
        builder.ToTable("PnlDetailPerOrders");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Period)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(d => d.OrderType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(d => d.OrderCategory)
            .HasMaxLength(50);

        builder.Property(d => d.InstallationMethod)
            .HasMaxLength(50);

        builder.Property(d => d.RevenueAmount)
            .HasPrecision(18, 2);

        builder.Property(d => d.MaterialCost)
            .HasPrecision(18, 2);

        builder.Property(d => d.LabourCost)
            .HasPrecision(18, 2);

        builder.Property(d => d.OverheadAllocated)
            .HasPrecision(18, 2);

        builder.Property(d => d.GrossProfit)
            .HasPrecision(18, 2);

        builder.Property(d => d.ProfitForOrder)
            .HasPrecision(18, 2);

        builder.Property(d => d.KpiResult)
            .HasMaxLength(50);

        builder.Property(d => d.RevenueRateSource)
            .HasMaxLength(100);

        builder.Property(d => d.LabourRateSource)
            .HasMaxLength(100);

        builder.Property(d => d.DataQualityNotes)
            .HasMaxLength(500);

        // Indexes for common queries
        builder.HasIndex(d => new { d.CompanyId, d.OrderId });
        builder.HasIndex(d => new { d.CompanyId, d.Period });
        builder.HasIndex(d => d.PartnerId);
        builder.HasIndex(d => d.DepartmentId);
        builder.HasIndex(d => d.ServiceInstallerId);
        builder.HasIndex(d => new { d.CompanyId, d.Period, d.OrderType });
    }
}

