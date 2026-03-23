using CephasOps.Domain.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Commands;

public class CommandProcessingLogConfiguration : IEntityTypeConfiguration<CommandProcessingLog>
{
    public void Configure(EntityTypeBuilder<CommandProcessingLog> builder)
    {
        builder.ToTable("CommandProcessingLogs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.IdempotencyKey).IsRequired().HasMaxLength(512);
        builder.Property(e => e.CommandType).IsRequired().HasMaxLength(512);
        builder.Property(e => e.CorrelationId).HasMaxLength(128);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(32);
        builder.Property(e => e.ResultJson).HasMaxLength(8000);
        builder.Property(e => e.ErrorMessage).HasMaxLength(2000);

        builder.HasIndex(e => e.IdempotencyKey).IsUnique();
        builder.HasIndex(e => new { e.Status, e.CreatedAtUtc });
    }
}
