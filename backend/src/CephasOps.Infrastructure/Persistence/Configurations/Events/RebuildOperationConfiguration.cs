using CephasOps.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Events;

public class RebuildOperationConfiguration : IEntityTypeConfiguration<RebuildOperation>
{
    public void Configure(EntityTypeBuilder<RebuildOperation> builder)
    {
        builder.ToTable("RebuildOperations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.RebuildTargetId).IsRequired().HasMaxLength(128);
        builder.Property(e => e.State).IsRequired().HasMaxLength(50);
        builder.Property(e => e.ErrorMessage).HasMaxLength(2000);
        builder.Property(e => e.Notes).HasMaxLength(2000);
        builder.Property(e => e.RerunReason).HasMaxLength(500);

        builder.HasIndex(e => e.RequestedAtUtc);
        builder.HasIndex(e => new { e.ScopeCompanyId, e.RequestedAtUtc });
        builder.HasIndex(e => e.RebuildTargetId);
        builder.HasIndex(e => new { e.State, e.RequestedAtUtc });
        builder.HasIndex(e => e.BackgroundJobId);
        builder.HasIndex(e => e.WorkerId);
    }
}
