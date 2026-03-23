using CephasOps.Domain.Payroll.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Payroll;

public class PayrollLineConfiguration : IEntityTypeConfiguration<PayrollLine>
{
    public void Configure(EntityTypeBuilder<PayrollLine> builder)
    {
        builder.ToTable("PayrollLines");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.TotalPay)
            .HasPrecision(18, 2);

        builder.Property(l => l.Adjustments)
            .HasPrecision(18, 2);

        builder.Property(l => l.NetPay)
            .HasPrecision(18, 2);

        builder.Property(l => l.ExportReference)
            .HasMaxLength(200);

        builder.HasOne(l => l.PayrollRun)
            .WithMany(r => r.PayrollLines)
            .HasForeignKey(l => l.PayrollRunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(l => new { l.CompanyId, l.PayrollRunId });
        builder.HasIndex(l => l.ServiceInstallerId);
    }
}

