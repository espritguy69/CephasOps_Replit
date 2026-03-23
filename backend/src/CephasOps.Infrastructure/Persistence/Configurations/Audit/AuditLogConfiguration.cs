using CephasOps.Domain.Audit.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Audit;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(al => al.Id);

        builder.Property(al => al.EntityType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(al => al.Action)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(al => al.Channel)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(al => al.FieldChangesJson)
            .HasColumnType("jsonb");

        builder.Property(al => al.MetadataJson)
            .HasColumnType("jsonb");

        builder.Property(al => al.IpAddress)
            .HasMaxLength(45);

        builder.HasIndex(al => new { al.CompanyId, al.EntityType, al.EntityId });
        builder.HasIndex(al => new { al.CompanyId, al.Timestamp });
        builder.HasIndex(al => new { al.UserId, al.Timestamp });
    }
}
