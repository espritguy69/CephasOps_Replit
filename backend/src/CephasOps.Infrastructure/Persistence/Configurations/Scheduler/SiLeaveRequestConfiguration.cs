using CephasOps.Domain.Scheduler.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Scheduler;

public class SiLeaveRequestConfiguration : IEntityTypeConfiguration<SiLeaveRequest>
{
    public void Configure(EntityTypeBuilder<SiLeaveRequest> builder)
    {
        builder.ToTable("SiLeaveRequests");

        builder.HasKey(slr => slr.Id);

        builder.Property(slr => slr.Reason)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(slr => slr.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(slr => new { slr.CompanyId, slr.ServiceInstallerId, slr.DateFrom, slr.DateTo });
        builder.HasIndex(slr => new { slr.CompanyId, slr.Status });
    }
}

