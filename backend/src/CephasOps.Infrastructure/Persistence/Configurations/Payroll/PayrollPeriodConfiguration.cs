using CephasOps.Domain.Payroll.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Payroll;

public class PayrollPeriodConfiguration : IEntityTypeConfiguration<PayrollPeriod>
{
    public void Configure(EntityTypeBuilder<PayrollPeriod> builder)
    {
        builder.ToTable("PayrollPeriods");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Period)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(p => p.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasMany(p => p.PayrollRuns)
            .WithOne(r => r.PayrollPeriod)
            .HasForeignKey(r => r.PayrollPeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.CompanyId, p.Period })
            .IsUnique();

        builder.HasIndex(p => new { p.CompanyId, p.Status });
    }
}

