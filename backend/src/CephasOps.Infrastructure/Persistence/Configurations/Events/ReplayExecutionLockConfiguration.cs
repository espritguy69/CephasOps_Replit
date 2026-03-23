using CephasOps.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Events;

public class ReplayExecutionLockConfiguration : IEntityTypeConfiguration<ReplayExecutionLock>
{
    public void Configure(EntityTypeBuilder<ReplayExecutionLock> builder)
    {
        builder.ToTable("ReplayExecutionLock");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.CompanyId);
        builder.HasIndex(e => e.ReplayOperationId);
        builder.HasIndex(e => e.ReleasedAtUtc);

        // Only one active lock per company: unique on CompanyId where ReleasedAtUtc IS NULL
        builder.HasIndex(e => e.CompanyId)
            .IsUnique()
            .HasFilter("\"ReleasedAtUtc\" IS NULL");
    }
}
