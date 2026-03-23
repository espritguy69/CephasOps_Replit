using CephasOps.Domain.Integration.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Integration;

public class ConnectorEndpointConfiguration : IEntityTypeConfiguration<ConnectorEndpoint>
{
    public void Configure(EntityTypeBuilder<ConnectorEndpoint> builder)
    {
        builder.ToTable("ConnectorEndpoints");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EndpointUrl).IsRequired().HasMaxLength(2048);
        builder.Property(e => e.HttpMethod).IsRequired().HasMaxLength(16);
        builder.Property(e => e.AllowedEventTypes).HasMaxLength(2000);
        builder.Property(e => e.SigningConfigJson).HasMaxLength(4000);
        builder.Property(e => e.AuthConfigJson).HasMaxLength(4000);

        builder.HasOne(e => e.ConnectorDefinition)
            .WithMany()
            .HasForeignKey(e => e.ConnectorDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.ConnectorDefinitionId, e.CompanyId });
        builder.HasIndex(e => e.CompanyId);
    }
}
