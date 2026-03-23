using CephasOps.Domain.Inventory.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Inventory;

public class MaterialConfiguration : IEntityTypeConfiguration<Material>
{
    public void Configure(EntityTypeBuilder<Material> builder)
    {
        builder.ToTable("Materials");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.ItemCode)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(m => m.Category)
            .HasMaxLength(100); // Legacy field - kept for backward compatibility

        builder.Property(m => m.MaterialCategoryId);

        builder.Property(m => m.UnitOfMeasure)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(m => m.VerticalFlags)
            .HasMaxLength(200); // Legacy field - kept for backward compatibility

        builder.Property(m => m.Barcode)
            .HasMaxLength(200);

        builder.HasIndex(m => new { m.CompanyId, m.ItemCode })
            .IsUnique();

        builder.HasIndex(m => new { m.CompanyId, m.Barcode })
            .IsUnique()
            .HasFilter("\"Barcode\" IS NOT NULL");

        builder.HasIndex(m => new { m.CompanyId, m.Category });
        builder.HasIndex(m => new { m.CompanyId, m.IsActive });
        builder.HasIndex(m => m.DepartmentId);
        builder.HasIndex(m => m.MaterialCategoryId);

        // Relationship to MaterialCategory
        builder.HasOne(m => m.MaterialCategory)
            .WithMany()
            .HasForeignKey(m => m.MaterialCategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        // Optional relationship to Department
        builder.HasOne<CephasOps.Domain.Departments.Entities.Department>()
            .WithMany()
            .HasForeignKey(m => m.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        // Optional relationship to Partner (legacy - for backward compatibility)
        builder.HasOne<CephasOps.Domain.Companies.Entities.Partner>()
            .WithMany()
            .HasForeignKey(m => m.PartnerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(m => m.PartnerId);

        // DO NOT configure MaterialPartners here - it's already configured in MaterialPartnerConfiguration
        // Configuring it here causes EF Core to create shadow properties (e.g., PartnerId1)
    }
}

