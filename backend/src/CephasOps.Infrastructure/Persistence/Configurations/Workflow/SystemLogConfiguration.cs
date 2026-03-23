using CephasOps.Domain.Workflow.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Workflow;

public class SystemLogConfiguration : IEntityTypeConfiguration<SystemLog>
{
    public void Configure(EntityTypeBuilder<SystemLog> builder)
    {
        builder.ToTable("SystemLogs");

        builder.HasKey(sl => sl.Id);

        builder.Property(sl => sl.Severity)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(sl => sl.Category)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(sl => sl.Message)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(sl => sl.DetailsJson)
            .HasColumnType("jsonb");

        builder.Property(sl => sl.EntityType)
            .HasMaxLength(100);

        builder.HasIndex(sl => new { sl.CompanyId, sl.Category, sl.CreatedAt });
        builder.HasIndex(sl => new { sl.Severity, sl.CreatedAt });
        builder.HasIndex(sl => new { sl.EntityType, sl.EntityId, sl.CreatedAt });
    }
}

