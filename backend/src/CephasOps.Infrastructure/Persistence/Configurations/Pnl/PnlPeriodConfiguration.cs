using CephasOps.Domain.Pnl.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Pnl;

public class PnlPeriodConfiguration : IEntityTypeConfiguration<PnlPeriod>
{
    public void Configure(EntityTypeBuilder<PnlPeriod> builder)
    {
        builder.ToTable("PnlPeriods");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Period)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasMany(p => p.PnlFacts)
            .WithOne(f => f.PnlPeriod)
            .HasForeignKey(f => f.PnlPeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.CompanyId, p.Period })
            .IsUnique();
    }
}

