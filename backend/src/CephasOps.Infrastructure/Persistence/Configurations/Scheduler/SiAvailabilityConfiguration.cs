using CephasOps.Domain.Scheduler.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Scheduler;

public class SiAvailabilityConfiguration : IEntityTypeConfiguration<SiAvailability>
{
    public void Configure(EntityTypeBuilder<SiAvailability> builder)
    {
        builder.ToTable("SiAvailabilities");

        builder.HasKey(sa => sa.Id);

        builder.Property(sa => sa.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(sa => new { sa.CompanyId, sa.ServiceInstallerId, sa.Date })
            .IsUnique();

        builder.HasIndex(sa => new { sa.CompanyId, sa.Date });
    }
}

