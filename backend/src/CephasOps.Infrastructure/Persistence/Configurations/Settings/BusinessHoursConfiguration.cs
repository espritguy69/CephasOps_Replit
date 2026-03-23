using CephasOps.Domain.Settings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Settings;

public class BusinessHoursConfiguration : IEntityTypeConfiguration<BusinessHours>
{
    public void Configure(EntityTypeBuilder<BusinessHours> builder)
    {
        builder.ToTable("business_hours");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.Description)
            .HasMaxLength(1000);

        builder.Property(b => b.Timezone)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue("Asia/Kuala_Lumpur");

        builder.Property(b => b.MondayStart)
            .HasMaxLength(5);

        builder.Property(b => b.MondayEnd)
            .HasMaxLength(5);

        builder.Property(b => b.TuesdayStart)
            .HasMaxLength(5);

        builder.Property(b => b.TuesdayEnd)
            .HasMaxLength(5);

        builder.Property(b => b.WednesdayStart)
            .HasMaxLength(5);

        builder.Property(b => b.WednesdayEnd)
            .HasMaxLength(5);

        builder.Property(b => b.ThursdayStart)
            .HasMaxLength(5);

        builder.Property(b => b.ThursdayEnd)
            .HasMaxLength(5);

        builder.Property(b => b.FridayStart)
            .HasMaxLength(5);

        builder.Property(b => b.FridayEnd)
            .HasMaxLength(5);

        builder.Property(b => b.SaturdayStart)
            .HasMaxLength(5);

        builder.Property(b => b.SaturdayEnd)
            .HasMaxLength(5);

        builder.Property(b => b.SundayStart)
            .HasMaxLength(5);

        builder.Property(b => b.SundayEnd)
            .HasMaxLength(5);

        builder.Property(b => b.IsDefault)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(b => b.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Indexes
        builder.HasIndex(b => b.CompanyId);
        builder.HasIndex(b => b.DepartmentId);
        builder.HasIndex(b => new { b.CompanyId, b.IsDefault });
        builder.HasIndex(b => new { b.CompanyId, b.IsActive, b.EffectiveFrom, b.EffectiveTo });
    }
}

public class PublicHolidayConfiguration : IEntityTypeConfiguration<PublicHoliday>
{
    public void Configure(EntityTypeBuilder<PublicHoliday> builder)
    {
        builder.ToTable("public_holidays");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(h => h.HolidayType)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("National");

        builder.Property(h => h.State)
            .HasMaxLength(100);

        builder.Property(h => h.Description)
            .HasMaxLength(500);

        builder.Property(h => h.IsRecurring)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(h => h.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Indexes
        builder.HasIndex(h => h.CompanyId);
        builder.HasIndex(h => h.HolidayDate);
        builder.HasIndex(h => h.HolidayType);
        builder.HasIndex(h => h.State);
        builder.HasIndex(h => new { h.CompanyId, h.HolidayDate });
        builder.HasIndex(h => new { h.CompanyId, h.IsActive, h.HolidayDate });
    }
}

