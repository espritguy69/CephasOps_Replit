using CephasOps.Domain.Workflow.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Workflow;

public class BackgroundJobConfiguration : IEntityTypeConfiguration<BackgroundJob>
{
    public void Configure(EntityTypeBuilder<BackgroundJob> builder)
    {
        builder.ToTable("BackgroundJobs");

        builder.HasKey(bj => bj.Id);

        builder.Property(bj => bj.JobType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(bj => bj.PayloadJson)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(bj => bj.State)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(bj => bj.LastError)
            .HasMaxLength(2000);

        builder.Property(bj => bj.RetriedFromJobRunId);

        builder.HasIndex(bj => new { bj.State, bj.Priority, bj.CreatedAt });
        builder.HasIndex(bj => new { bj.JobType, bj.State });
        builder.HasIndex(bj => bj.ScheduledAt)
            .HasFilter("\"ScheduledAt\" IS NOT NULL");
        builder.HasIndex(bj => bj.WorkerId);
        builder.HasIndex(bj => new { bj.State, bj.WorkerId });
        builder.Property(bj => bj.CompanyId);
        builder.HasIndex(bj => bj.CompanyId);
        builder.HasIndex(bj => new { bj.CompanyId, bj.State, bj.CreatedAt });
    }
}

