using CephasOps.Domain.Scheduler.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Scheduler;

public class ScheduledSlotConfiguration : IEntityTypeConfiguration<ScheduledSlot>
{
    public void Configure(EntityTypeBuilder<ScheduledSlot> builder)
    {
        builder.ToTable("ScheduledSlots");

        builder.HasKey(ss => ss.Id);

        builder.Property(ss => ss.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(ss => new { ss.CompanyId, ss.ServiceInstallerId, ss.Date });
        builder.HasIndex(ss => new { ss.CompanyId, ss.OrderId });
        builder.HasIndex(ss => new { ss.CompanyId, ss.Date, ss.Status });
    }
}

