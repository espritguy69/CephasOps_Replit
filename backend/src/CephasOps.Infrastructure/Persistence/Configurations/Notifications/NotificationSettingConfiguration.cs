using CephasOps.Domain.Notifications.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Notifications;

public class NotificationSettingConfiguration : IEntityTypeConfiguration<NotificationSetting>
{
    public void Configure(EntityTypeBuilder<NotificationSetting> builder)
    {
        builder.ToTable("NotificationSettings");

        builder.HasKey(ns => ns.Id);

        builder.Property(ns => ns.NotificationType)
            .HasMaxLength(50);

        builder.Property(ns => ns.Channel)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(ns => ns.MinimumPriority)
            .HasMaxLength(20);

        builder.Property(ns => ns.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(ns => new { ns.UserId, ns.CompanyId, ns.NotificationType });
        builder.HasIndex(ns => new { ns.CompanyId, ns.NotificationType });
    }
}

