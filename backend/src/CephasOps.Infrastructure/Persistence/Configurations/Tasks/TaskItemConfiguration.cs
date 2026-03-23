using CephasOps.Domain.Tasks.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Tasks;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("TaskItems");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(t => t.Description)
            .HasMaxLength(4000);

        builder.Property(t => t.Priority)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(t => t.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(t => new { t.CompanyId, t.AssignedToUserId, t.Status });
        builder.HasIndex(t => new { t.CompanyId, t.DepartmentId, t.Status });
        builder.HasIndex(t => new { t.CompanyId, t.RequestedByUserId });
        builder.HasIndex(t => new { t.CompanyId, t.OrderId }).HasFilter("\"OrderId\" IS NOT NULL");
    }
}

