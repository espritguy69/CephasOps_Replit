using CephasOps.Domain.Rates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Rates;

public class ServiceProfileConfiguration : IEntityTypeConfiguration<ServiceProfile>
{
    public void Configure(EntityTypeBuilder<ServiceProfile> builder)
    {
        builder.ToTable("ServiceProfiles");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(50);
        builder.Property(x => x.Name).HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(500);

        builder.HasIndex(x => new { x.CompanyId, x.Code })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(x => new { x.CompanyId, x.IsActive })
            .HasFilter("\"IsDeleted\" = false");
    }
}
