using CephasOps.Domain.Operations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Operations;

public class MigrationAuditConfiguration : IEntityTypeConfiguration<MigrationAudit>
{
    public void Configure(EntityTypeBuilder<MigrationAudit> builder)
    {
        builder.ToTable("MigrationAudit");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Environment)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.MigrationId)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(a => a.AppliedBy)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(a => a.MethodUsed)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.VerificationStatus)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.SmokeTestStatus)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.Notes)
            .HasColumnType("text");

        builder.HasIndex(a => new { a.Environment, a.MigrationId });
        builder.HasIndex(a => a.AppliedAtUtc);
    }
}
