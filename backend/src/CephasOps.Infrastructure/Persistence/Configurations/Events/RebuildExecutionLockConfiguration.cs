using CephasOps.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Events;

public class RebuildExecutionLockConfiguration : IEntityTypeConfiguration<RebuildExecutionLock>
{
    public void Configure(EntityTypeBuilder<RebuildExecutionLock> builder)
    {
        builder.ToTable("RebuildExecutionLocks");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.RebuildTargetId).IsRequired().HasMaxLength(128);
        builder.Property(e => e.ScopeKey).IsRequired().HasMaxLength(64);

        builder.HasIndex(e => new { e.RebuildTargetId, e.ScopeKey })
            .IsUnique()
            .HasFilter("\"ReleasedAtUtc\" IS NULL");
        builder.HasIndex(e => e.ReleasedAtUtc);
        builder.HasIndex(e => e.RebuildOperationId);
    }
}
