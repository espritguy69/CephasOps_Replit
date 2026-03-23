using CephasOps.Domain.Payroll.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Payroll;

public class PayrollRunConfiguration : IEntityTypeConfiguration<PayrollRun>
{
    public void Configure(EntityTypeBuilder<PayrollRun> builder)
    {
        builder.ToTable("PayrollRuns");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.TotalAmount)
            .HasPrecision(18, 2);

        builder.Property(r => r.Notes)
            .HasMaxLength(1000);

        builder.Property(r => r.ExportReference)
            .HasMaxLength(200);

        builder.HasOne(r => r.PayrollPeriod)
            .WithMany(p => p.PayrollRuns)
            .HasForeignKey(r => r.PayrollPeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.PayrollLines)
            .WithOne(l => l.PayrollRun)
            .HasForeignKey(l => l.PayrollRunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.JobEarningRecords)
            .WithOne(j => j.PayrollRun)
            .HasForeignKey(j => j.PayrollRunId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(r => new { r.CompanyId, r.PayrollPeriodId });
        builder.HasIndex(r => new { r.CompanyId, r.Status });
        builder.HasIndex(r => new { r.CompanyId, r.PeriodStart, r.PeriodEnd });
    }
}

