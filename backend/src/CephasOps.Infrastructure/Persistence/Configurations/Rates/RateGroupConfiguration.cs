using CephasOps.Domain.Rates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Rates;

public class RateGroupConfiguration : IEntityTypeConfiguration<RateGroup>
{
    public void Configure(EntityTypeBuilder<RateGroup> builder)
    {
        builder.ToTable("RateGroups");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Name).IsRequired().HasMaxLength(200);
        builder.Property(r => r.Code).IsRequired().HasMaxLength(50);
        builder.Property(r => r.Description).HasMaxLength(500);
        builder.HasIndex(r => new { r.CompanyId, r.Code }).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(r => new { r.CompanyId, r.IsActive });
    }
}
