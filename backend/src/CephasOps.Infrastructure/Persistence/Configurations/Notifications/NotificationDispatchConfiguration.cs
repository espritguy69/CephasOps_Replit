using CephasOps.Domain.Notifications.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Notifications;

public class NotificationDispatchConfiguration : IEntityTypeConfiguration<NotificationDispatch>
{
    public void Configure(EntityTypeBuilder<NotificationDispatch> builder)
    {
        builder.ToTable("NotificationDispatches");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Channel).IsRequired().HasMaxLength(50);
        builder.Property(d => d.Target).IsRequired().HasMaxLength(500);
        builder.Property(d => d.TemplateKey).HasMaxLength(200);
        builder.Property(d => d.PayloadJson).HasColumnType("jsonb");
        builder.Property(d => d.Status).IsRequired().HasMaxLength(50);
        builder.Property(d => d.LastError).HasMaxLength(2000);
        builder.Property(d => d.CorrelationId).HasMaxLength(200);
        builder.Property(d => d.IdempotencyKey).HasMaxLength(500);
        builder.Property(d => d.ProcessingNodeId).HasMaxLength(200);

        builder.HasIndex(d => new { d.Status, d.NextRetryAtUtc }).HasDatabaseName("IX_NotificationDispatches_Status_NextRetryAtUtc");
        builder.HasIndex(d => new { d.CompanyId, d.Status }).HasDatabaseName("IX_NotificationDispatches_CompanyId_Status");
        builder.HasIndex(d => d.IdempotencyKey).HasDatabaseName("IX_NotificationDispatches_IdempotencyKey").IsUnique().HasFilter("\"IdempotencyKey\" IS NOT NULL");
    }
}
