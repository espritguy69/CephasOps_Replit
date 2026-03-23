using CephasOps.Domain.Tenants.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Tenants;

public class TenantActivityEventConfiguration : IEntityTypeConfiguration<TenantActivityEvent>
{
    public void Configure(EntityTypeBuilder<TenantActivityEvent> builder)
    {
        builder.ToTable("TenantActivityEvents");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TenantId, x.TimestampUtc });
    }
}
