using CephasOps.Domain.Companies.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Companies;

public class VerticalConfiguration : IEntityTypeConfiguration<Vertical>
{
    public void Configure(EntityTypeBuilder<Vertical> builder)
    {
        builder.ToTable("Verticals");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(v => v.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(v => v.Description)
            .HasMaxLength(500);

        builder.Property(v => v.DisplayOrder)
            .HasDefaultValue(0);

        // Indexes
        builder.HasIndex(v => new { v.CompanyId, v.Code })
            .IsUnique();

        builder.HasIndex(v => new { v.CompanyId, v.IsActive });
    }
}

