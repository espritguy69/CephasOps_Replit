using CephasOps.Domain.Buildings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Buildings;

public class BuildingDefaultMaterialConfiguration : IEntityTypeConfiguration<BuildingDefaultMaterial>
{
    public void Configure(EntityTypeBuilder<BuildingDefaultMaterial> builder)
    {
        builder.ToTable("BuildingDefaultMaterials");

        builder.HasKey(bdm => bdm.Id);

        builder.Property(bdm => bdm.DefaultQuantity)
            .HasPrecision(18, 2);

        builder.Property(bdm => bdm.Notes)
            .HasMaxLength(1000);

        // Relationship to Building (optional to avoid query filter warnings)
        builder.HasOne(bdm => bdm.Building)
            .WithMany()
            .HasForeignKey(bdm => bdm.BuildingId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(bdm => bdm.BuildingId);
        builder.HasIndex(bdm => new { bdm.BuildingId, bdm.OrderTypeId });
        builder.HasIndex(bdm => new { bdm.BuildingId, bdm.MaterialId });
        builder.HasIndex(bdm => new { bdm.BuildingId, bdm.IsActive });
    }
}

