using CephasOps.Domain.Integration.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Integration;

public class ExternalIdempotencyRecordConfiguration : IEntityTypeConfiguration<ExternalIdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<ExternalIdempotencyRecord> builder)
    {
        builder.ToTable("ExternalIdempotencyRecords");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.IdempotencyKey).IsRequired().HasMaxLength(512);
        builder.Property(e => e.ConnectorKey).IsRequired().HasMaxLength(128);

        // Tenant-safe: same external key can be used per (ConnectorKey, CompanyId) so different tenants do not collide.
        builder.HasIndex(e => new { e.ConnectorKey, e.CompanyId, e.IdempotencyKey }).IsUnique();
        builder.HasIndex(e => new { e.ConnectorKey, e.CompletedAtUtc });
    }
}
