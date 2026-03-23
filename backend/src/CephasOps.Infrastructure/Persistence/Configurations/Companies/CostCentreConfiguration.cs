using CephasOps.Domain.Companies.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Companies;

public class CostCentreConfiguration : IEntityTypeConfiguration<CostCentre>
{
    public void Configure(EntityTypeBuilder<CostCentre> builder)
    {
        builder.ToTable("CostCentres");

        builder.HasKey(cc => cc.Id);

        builder.Property(cc => cc.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(cc => cc.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(cc => cc.Description)
            .HasMaxLength(1000);

        builder.HasIndex(cc => new { cc.CompanyId, cc.Code })
            .IsUnique();

        builder.HasIndex(cc => new { cc.CompanyId, cc.IsActive });
    }
}

