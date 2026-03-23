using CephasOps.Domain.Payroll.Entities;
using CephasOps.Domain.Orders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Payroll;

public class JobEarningRecordConfiguration : IEntityTypeConfiguration<JobEarningRecord>
{
    public void Configure(EntityTypeBuilder<JobEarningRecord> builder)
    {
        builder.ToTable("JobEarningRecords");

        builder.HasKey(j => j.Id);

        // OrderType relationship
        builder.HasOne<CephasOps.Domain.Orders.Entities.OrderType>()
            .WithMany()
            .HasForeignKey(j => j.OrderTypeId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(j => j.OrderTypeCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(j => j.OrderTypeName)
            .IsRequired()
            .HasMaxLength(100);

        // JobType is deprecated - no longer populated, kept for backward compatibility
#pragma warning disable CS0618 // Type or member is obsolete
        builder.Property(j => j.JobType)
            .IsRequired(false) // Nullable - field is deprecated
            .HasMaxLength(50);
#pragma warning restore CS0618 // Type or member is obsolete

        builder.Property(j => j.KpiResult)
            .HasMaxLength(50);

        builder.Property(j => j.BaseRate)
            .HasPrecision(18, 2);

        builder.Property(j => j.KpiAdjustment)
            .HasPrecision(18, 2);

        builder.Property(j => j.FinalPay)
            .HasPrecision(18, 2);

        builder.Property(j => j.Period)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(j => j.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasOne(j => j.PayrollRun)
            .WithMany(r => r.JobEarningRecords)
            .HasForeignKey(j => j.PayrollRunId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(j => new { j.CompanyId, j.OrderId });
        builder.HasIndex(j => new { j.CompanyId, j.ServiceInstallerId });
        builder.HasIndex(j => new { j.CompanyId, j.Period });
        builder.HasIndex(j => j.PayrollRunId);
        builder.HasIndex(j => j.OrderTypeId);
    }
}

