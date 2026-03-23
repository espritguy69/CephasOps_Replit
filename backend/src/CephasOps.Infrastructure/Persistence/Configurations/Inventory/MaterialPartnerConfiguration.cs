using CephasOps.Domain.Inventory.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Inventory;

public class MaterialPartnerConfiguration : IEntityTypeConfiguration<MaterialPartner>
{
    public void Configure(EntityTypeBuilder<MaterialPartner> builder)
    {
        builder.ToTable("MaterialPartners");

        builder.HasKey(mp => mp.Id);

        // Unique constraint: one Material-Partner combination per company
        builder.HasIndex(mp => new { mp.CompanyId, mp.MaterialId, mp.PartnerId })
            .IsUnique();

        // Indexes for performance
        builder.HasIndex(mp => mp.MaterialId);
        builder.HasIndex(mp => mp.PartnerId);
        builder.HasIndex(mp => mp.CompanyId);

        // Relationships
        // Make Material relationship optional to avoid query filter issues with soft-deleted materials
        builder.HasOne(mp => mp.Material)
            .WithMany(m => m.MaterialPartners)
            .HasForeignKey(mp => mp.MaterialId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        // Use explicit navigation property to avoid shadow property conflicts
        builder.HasOne(mp => mp.Partner)
            .WithMany()
            .HasForeignKey(mp => mp.PartnerId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

