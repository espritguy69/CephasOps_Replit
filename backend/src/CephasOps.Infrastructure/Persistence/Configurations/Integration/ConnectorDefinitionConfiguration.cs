using CephasOps.Domain.Integration.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Integration;

public class ConnectorDefinitionConfiguration : IEntityTypeConfiguration<ConnectorDefinition>
{
    public void Configure(EntityTypeBuilder<ConnectorDefinition> builder)
    {
        builder.ToTable("ConnectorDefinitions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ConnectorKey).IsRequired().HasMaxLength(128);
        builder.Property(e => e.DisplayName).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Description).HasMaxLength(1024);
        builder.Property(e => e.ConnectorType).IsRequired().HasMaxLength(64);
        builder.Property(e => e.Direction).IsRequired().HasMaxLength(32);

        builder.HasIndex(e => e.ConnectorKey).IsUnique();
    }
}
