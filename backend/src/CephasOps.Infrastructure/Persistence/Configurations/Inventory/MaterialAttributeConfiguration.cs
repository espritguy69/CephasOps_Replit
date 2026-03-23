using CephasOps.Domain.Inventory.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Inventory;

public class MaterialAttributeConfiguration : IEntityTypeConfiguration<MaterialAttribute>
{
    public void Configure(EntityTypeBuilder<MaterialAttribute> builder)
    {
        builder.ToTable("MaterialAttributes");

        builder.HasKey(ma => ma.Id);

        builder.Property(ma => ma.Key)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(ma => ma.Value)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(ma => ma.DataType)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("String");

        builder.HasIndex(ma => ma.MaterialId);
        builder.HasIndex(ma => new { ma.MaterialId, ma.Key });

        // Relationship to Material
        builder.HasOne(ma => ma.Material)
            .WithMany(m => m.MaterialAttributes)
            .HasForeignKey(ma => ma.MaterialId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

