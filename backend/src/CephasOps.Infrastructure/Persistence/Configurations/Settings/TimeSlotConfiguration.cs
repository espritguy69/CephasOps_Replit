using CephasOps.Domain.Settings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Settings;

public class TimeSlotConfiguration : IEntityTypeConfiguration<TimeSlot>
{
    public void Configure(EntityTypeBuilder<TimeSlot> builder)
    {
        builder.ToTable("TimeSlots");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Time)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(t => t.SortOrder)
            .IsRequired();

        builder.Property(t => t.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Indexes
        builder.HasIndex(t => t.CompanyId);
        builder.HasIndex(t => t.SortOrder);
        builder.HasIndex(t => new { t.CompanyId, t.Time })
            .IsUnique();
    }
}

