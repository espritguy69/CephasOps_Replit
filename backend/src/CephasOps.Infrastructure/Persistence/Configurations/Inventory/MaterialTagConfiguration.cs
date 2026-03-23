using CephasOps.Domain.Inventory.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Inventory;

public class MaterialTagConfiguration : IEntityTypeConfiguration<MaterialTag>
{
    public void Configure(EntityTypeBuilder<MaterialTag> builder)
    {
        builder.ToTable("MaterialTags");

        builder.HasKey(mt => mt.Id);

        builder.Property(mt => mt.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(mt => mt.Color)
            .HasMaxLength(7); // Hex color code

        builder.HasIndex(mt => new { mt.CompanyId, mt.Name })
            .IsUnique();

        builder.HasIndex(mt => new { mt.CompanyId, mt.IsActive });

        // Many-to-many relationship with Material
        builder.HasMany(mt => mt.Materials)
            .WithMany(m => m.MaterialTags)
            .UsingEntity<Dictionary<string, object>>(
                "MaterialMaterialTags",
                j => j.HasOne<Material>().WithMany().HasForeignKey("MaterialId"),
                j => j.HasOne<MaterialTag>().WithMany().HasForeignKey("MaterialTagId"),
                j =>
                {
                    j.HasKey("MaterialId", "MaterialTagId");
                    j.ToTable("MaterialMaterialTags");
                });
    }
}

