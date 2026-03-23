using CephasOps.Domain.Integration.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Integration;

public class OutboundIntegrationDeliveryConfiguration : IEntityTypeConfiguration<OutboundIntegrationDelivery>
{
    public void Configure(EntityTypeBuilder<OutboundIntegrationDelivery> builder)
    {
        builder.ToTable("OutboundIntegrationDeliveries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EventType).IsRequired().HasMaxLength(256);
        builder.Property(e => e.CorrelationId).HasMaxLength(128);
        builder.Property(e => e.IdempotencyKey).IsRequired().HasMaxLength(512);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(32);
        builder.Property(e => e.PayloadJson).IsRequired().HasColumnType("jsonb");
        builder.Property(e => e.SignatureHeaderValue).HasMaxLength(512);
        builder.Property(e => e.LastErrorMessage).HasMaxLength(2000);

        builder.HasIndex(e => e.IdempotencyKey).IsUnique();
        builder.HasIndex(e => new { e.ConnectorEndpointId, e.Status, e.CreatedAtUtc });
        builder.HasIndex(e => new { e.CompanyId, e.Status, e.CreatedAtUtc });
        builder.HasIndex(e => new { e.EventType, e.Status });
        builder.HasIndex(e => e.NextRetryAtUtc).HasFilter("\"NextRetryAtUtc\" IS NOT NULL");
    }
}
