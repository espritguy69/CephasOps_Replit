using CephasOps.Domain.Buildings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Buildings;

public class InstallationMethodConfiguration : IEntityTypeConfiguration<InstallationMethod>
{
    public void Configure(EntityTypeBuilder<InstallationMethod> builder)
    {
        builder.ToTable("InstallationMethods");

        builder.HasKey(im => im.Id);

        builder.Property(im => im.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(im => im.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(im => im.Category)
            .HasMaxLength(50);

        builder.Property(im => im.Description)
            .HasMaxLength(1000);

        // Indexes
        builder.HasIndex(im => new { im.CompanyId, im.Code }).IsUnique();
        builder.HasIndex(im => new { im.CompanyId, im.IsActive });
        builder.HasIndex(im => new { im.CompanyId, im.Category });
        builder.HasIndex(im => im.DisplayOrder);
    }
}

