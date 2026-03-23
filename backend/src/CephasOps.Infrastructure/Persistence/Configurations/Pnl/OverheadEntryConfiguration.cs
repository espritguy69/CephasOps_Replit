using CephasOps.Domain.Pnl.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Pnl;

public class OverheadEntryConfiguration : IEntityTypeConfiguration<OverheadEntry>
{
    public void Configure(EntityTypeBuilder<OverheadEntry> builder)
    {
        builder.ToTable("OverheadEntries");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Period)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(o => o.Amount)
            .HasPrecision(18, 2);

        builder.Property(o => o.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(o => o.AllocationBasis)
            .HasMaxLength(200);

        builder.HasIndex(o => new { o.CompanyId, o.CostCentreId });
        builder.HasIndex(o => new { o.CompanyId, o.Period });
    }
}

