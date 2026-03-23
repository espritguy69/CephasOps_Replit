using CephasOps.Domain.Notifications.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Notifications;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Type)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(n => n.Priority)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(n => n.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(n => n.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(n => n.Message)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(n => n.ActionUrl)
            .HasMaxLength(1000);

        builder.Property(n => n.ActionText)
            .HasMaxLength(200);

        builder.Property(n => n.RelatedEntityType)
            .HasMaxLength(100);

        builder.Property(n => n.MetadataJson)
            .HasColumnType("jsonb");

        builder.Property(n => n.DeliveryChannels)
            .HasMaxLength(200);

        builder.HasIndex(n => new { n.UserId, n.CompanyId, n.Status });
        builder.HasIndex(n => new { n.CompanyId, n.Type, n.CreatedAt });
        builder.HasIndex(n => new { n.RelatedEntityId, n.RelatedEntityType });
    }
}

