using CephasOps.Domain.Audit.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Audit;

public class AuditOverrideConfiguration : IEntityTypeConfiguration<AuditOverride>
{
    public void Configure(EntityTypeBuilder<AuditOverride> builder)
    {
        builder.ToTable("AuditOverrides");

        builder.HasKey(ao => ao.Id);

        builder.Property(ao => ao.EntityType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(ao => ao.OverrideType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(ao => ao.OriginalValue)
            .HasMaxLength(4000);

        builder.Property(ao => ao.NewValue)
            .HasMaxLength(4000);

        builder.Property(ao => ao.Reason)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(ao => ao.EvidenceNotes)
            .HasMaxLength(2000);

        builder.Property(ao => ao.OverriddenByRole)
            .IsRequired()
            .HasMaxLength(50);

        // Indexes for efficient querying
        builder.HasIndex(ao => new { ao.CompanyId, ao.EntityType, ao.EntityId });
        builder.HasIndex(ao => new { ao.CompanyId, ao.OverriddenByUserId, ao.OverriddenAt });
        builder.HasIndex(ao => ao.OverrideType);
        builder.HasIndex(ao => ao.OverriddenAt);
    }
}

