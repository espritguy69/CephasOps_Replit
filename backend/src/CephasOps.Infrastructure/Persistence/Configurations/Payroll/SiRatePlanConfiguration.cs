using CephasOps.Domain.Payroll.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Payroll;

public class SiRatePlanConfiguration : IEntityTypeConfiguration<SiRatePlan>
{
    public void Configure(EntityTypeBuilder<SiRatePlan> builder)
    {
        builder.ToTable("SiRatePlans");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Level)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.ActivationRate)
            .HasPrecision(18, 2);

        builder.Property(s => s.ModificationRate)
            .HasPrecision(18, 2);

        builder.Property(s => s.AssuranceRate)
            .HasPrecision(18, 2);

        builder.Property(s => s.FttrRate)
            .HasPrecision(18, 2);

        builder.Property(s => s.FttcRate)
            .HasPrecision(18, 2);

        builder.Property(s => s.SduRate)
            .HasPrecision(18, 2);

        builder.Property(s => s.RdfPoleRate)
            .HasPrecision(18, 2);

        builder.Property(s => s.OnTimeBonus)
            .HasPrecision(18, 2);

        builder.Property(s => s.LatePenalty)
            .HasPrecision(18, 2);

        builder.Property(s => s.ReworkRate)
            .HasPrecision(18, 2);

        builder.HasIndex(s => new { s.CompanyId, s.ServiceInstallerId, s.IsActive });
    }
}

