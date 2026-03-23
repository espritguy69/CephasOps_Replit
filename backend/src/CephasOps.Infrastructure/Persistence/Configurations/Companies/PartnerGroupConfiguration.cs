using CephasOps.Domain.Companies.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Companies;

public class PartnerGroupConfiguration : IEntityTypeConfiguration<PartnerGroup>
{
    public void Configure(EntityTypeBuilder<PartnerGroup> builder)
    {
        builder.ToTable("PartnerGroups");

        builder.HasKey(pg => pg.Id);

        builder.Property(pg => pg.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(pg => new { pg.CompanyId, pg.Name });
    }
}

