using CephasOps.Domain.Inventory.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Inventory;

public class MaterialVerticalConfiguration : IEntityTypeConfiguration<MaterialVertical>
{
    public void Configure(EntityTypeBuilder<MaterialVertical> builder)
    {
        builder.ToTable("MaterialVerticals");

        builder.HasKey(mv => mv.Id);

        builder.Property(mv => mv.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(mv => mv.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(mv => new { mv.CompanyId, mv.Code })
            .IsUnique();

        builder.HasIndex(mv => new { mv.CompanyId, mv.IsActive });

        // Many-to-many relationship with Material
        builder.HasMany(mv => mv.Materials)
            .WithMany(m => m.MaterialVerticals)
            .UsingEntity<Dictionary<string, object>>(
                "MaterialMaterialVerticals",
                j => j.HasOne<Material>().WithMany().HasForeignKey("MaterialId"),
                j => j.HasOne<MaterialVertical>().WithMany().HasForeignKey("MaterialVerticalId"),
                j =>
                {
                    j.HasKey("MaterialId", "MaterialVerticalId");
                    j.ToTable("MaterialMaterialVerticals");
                });
    }
}

