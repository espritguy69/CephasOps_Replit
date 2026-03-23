using CephasOps.Domain.Settings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Settings;

public class SlaProfileConfiguration : IEntityTypeConfiguration<SlaProfile>
{
    public void Configure(EntityTypeBuilder<SlaProfile> builder)
    {
        builder.ToTable("sla_profiles");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Description)
            .HasMaxLength(1000);

        builder.Property(s => s.OrderType)
            .HasMaxLength(100);

        builder.Property(s => s.ResponseSlaFromStatus)
            .HasMaxLength(50);

        builder.Property(s => s.ResponseSlaToStatus)
            .HasMaxLength(50);

        builder.Property(s => s.ResolutionSlaFromStatus)
            .HasMaxLength(50);

        builder.Property(s => s.ResolutionSlaToStatus)
            .HasMaxLength(50);

        builder.Property(s => s.EscalationRole)
            .HasMaxLength(100);

        builder.Property(s => s.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(s => s.IsDefault)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(s => s.IsVipOnly)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(s => s.ExcludeNonBusinessHours)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(s => s.ExcludeWeekends)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(s => s.ExcludePublicHolidays)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(s => s.NotifyOnEscalation)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(s => s.NotifyOnBreach)
            .IsRequired()
            .HasDefaultValue(true);

        // Indexes
        builder.HasIndex(s => s.CompanyId);
        builder.HasIndex(s => s.PartnerId);
        builder.HasIndex(s => s.DepartmentId);
        builder.HasIndex(s => s.OrderType);
        builder.HasIndex(s => new { s.CompanyId, s.OrderType, s.PartnerId, s.DepartmentId });
        builder.HasIndex(s => new { s.CompanyId, s.IsDefault, s.OrderType });
    }
}

