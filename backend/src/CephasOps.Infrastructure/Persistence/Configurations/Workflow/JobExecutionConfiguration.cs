using CephasOps.Domain.Workflow.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Workflow;

public class JobExecutionConfiguration : IEntityTypeConfiguration<JobExecution>
{
    public void Configure(EntityTypeBuilder<JobExecution> builder)
    {
        builder.ToTable("JobExecutions");
        builder.HasKey(j => j.Id);
        builder.Property(j => j.JobType).IsRequired().HasMaxLength(200);
        builder.Property(j => j.PayloadJson).HasColumnType("jsonb");
        builder.Property(j => j.Status).IsRequired().HasMaxLength(50);
        builder.Property(j => j.LastError).HasMaxLength(2000);
        builder.Property(j => j.CorrelationId).HasMaxLength(200);
        builder.Property(j => j.ProcessingNodeId).HasMaxLength(200);

        builder.HasIndex(j => new { j.Status, j.NextRunAtUtc }).HasDatabaseName("IX_JobExecutions_Status_NextRunAtUtc");
        builder.HasIndex(j => new { j.CompanyId, j.Status }).HasDatabaseName("IX_JobExecutions_CompanyId_Status");
        builder.HasIndex(j => new { j.CompanyId, j.CreatedAtUtc }).HasDatabaseName("IX_JobExecutions_CompanyId_CreatedAtUtc");
    }
}
