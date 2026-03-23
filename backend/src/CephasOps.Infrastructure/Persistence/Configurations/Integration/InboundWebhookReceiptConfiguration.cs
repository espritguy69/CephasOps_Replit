using CephasOps.Domain.Integration.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Integration;

public class InboundWebhookReceiptConfiguration : IEntityTypeConfiguration<InboundWebhookReceipt>
{
    public void Configure(EntityTypeBuilder<InboundWebhookReceipt> builder)
    {
        builder.ToTable("InboundWebhookReceipts");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ExternalIdempotencyKey).IsRequired().HasMaxLength(512);
        builder.Property(e => e.ExternalEventId).HasMaxLength(256);
        builder.Property(e => e.ConnectorKey).IsRequired().HasMaxLength(128);
        builder.Property(e => e.MessageType).HasMaxLength(128);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(32);
        builder.Property(e => e.PayloadJson).IsRequired().HasColumnType("jsonb");
        builder.Property(e => e.CorrelationId).HasMaxLength(128);
        builder.Property(e => e.VerificationFailureReason).HasMaxLength(2000);
        builder.Property(e => e.HandlerErrorMessage).HasMaxLength(2000);

        builder.HasIndex(e => new { e.ConnectorKey, e.ExternalIdempotencyKey }).IsUnique();
        builder.HasIndex(e => new { e.ConnectorKey, e.Status, e.ReceivedAtUtc });
        builder.HasIndex(e => e.CompanyId);
    }
}
