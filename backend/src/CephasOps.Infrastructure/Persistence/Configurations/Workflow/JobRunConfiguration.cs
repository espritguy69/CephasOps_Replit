using CephasOps.Domain.Workflow.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Workflow;

public class JobRunConfiguration : IEntityTypeConfiguration<JobRun>
{
    public void Configure(EntityTypeBuilder<JobRun> builder)
    {
        builder.ToTable("JobRuns");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.JobName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.JobType).IsRequired().HasMaxLength(100);
        builder.Property(e => e.TriggerSource).IsRequired().HasMaxLength(50);
        builder.Property(e => e.CorrelationId).HasMaxLength(100);
        builder.Property(e => e.QueueOrChannel).HasMaxLength(100);
        builder.Property(e => e.PayloadSummary).HasMaxLength(1000);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(50);
        builder.Property(e => e.WorkerNode).HasMaxLength(256);
        builder.Property(e => e.ErrorCode).HasMaxLength(100);
        builder.Property(e => e.ErrorMessage).HasMaxLength(500);
        builder.Property(e => e.ErrorDetails).HasMaxLength(2000);
        builder.Property(e => e.RelatedEntityType).HasMaxLength(100);
        builder.Property(e => e.RelatedEntityId).HasMaxLength(50);

        builder.HasIndex(e => e.StartedAtUtc);
        builder.HasIndex(e => new { e.Status, e.StartedAtUtc });
        builder.HasIndex(e => new { e.JobType, e.StartedAtUtc });
        builder.HasIndex(e => new { e.CompanyId, e.StartedAtUtc });
        builder.HasIndex(e => e.BackgroundJobId).HasFilter("\"BackgroundJobId\" IS NOT NULL");
        builder.HasIndex(e => e.CorrelationId).HasFilter("\"CorrelationId\" IS NOT NULL");
        builder.HasIndex(e => e.ParentJobRunId).HasFilter("\"ParentJobRunId\" IS NOT NULL");
        builder.HasIndex(e => e.EventId).HasFilter("\"EventId\" IS NOT NULL");
    }
}
