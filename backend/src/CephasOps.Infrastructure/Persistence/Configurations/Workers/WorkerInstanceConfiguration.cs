using CephasOps.Domain.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Workers;

public class WorkerInstanceConfiguration : IEntityTypeConfiguration<WorkerInstance>
{
    public void Configure(EntityTypeBuilder<WorkerInstance> builder)
    {
        builder.ToTable("WorkerInstances");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.HostName).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Role).IsRequired().HasMaxLength(50);
        builder.HasIndex(e => e.LastHeartbeatUtc);
        builder.HasIndex(e => new { e.IsActive, e.LastHeartbeatUtc });
    }
}
