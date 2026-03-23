using CephasOps.Domain.Assets.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Assets;

public class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("Assets");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.AssetTag)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Description)
            .HasMaxLength(1000);

        builder.Property(a => a.SerialNumber)
            .HasMaxLength(100);

        builder.Property(a => a.ModelNumber)
            .HasMaxLength(100);

        builder.Property(a => a.Manufacturer)
            .HasMaxLength(100);

        builder.Property(a => a.Supplier)
            .HasMaxLength(200);

        builder.Property(a => a.PurchaseCost)
            .HasPrecision(18, 2);

        builder.Property(a => a.SalvageValue)
            .HasPrecision(18, 2);

        builder.Property(a => a.CurrentBookValue)
            .HasPrecision(18, 2);

        builder.Property(a => a.AccumulatedDepreciation)
            .HasPrecision(18, 2);

        builder.Property(a => a.DepreciationMethod)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(a => a.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(a => a.Location)
            .HasMaxLength(200);

        builder.Property(a => a.InsurancePolicyNumber)
            .HasMaxLength(100);

        builder.Property(a => a.Notes)
            .HasMaxLength(2000);

        builder.Property(a => a.IsFullyDepreciated)
            .HasDefaultValue(false);

        // Relationships
        builder.HasOne(a => a.AssetType)
            .WithMany(t => t.Assets)
            .HasForeignKey(a => a.AssetTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Disposal)
            .WithOne(d => d.Asset)
            .HasForeignKey<AssetDisposal>(d => d.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(a => new { a.CompanyId, a.AssetTag }).IsUnique();
        builder.HasIndex(a => new { a.CompanyId, a.AssetTypeId });
        builder.HasIndex(a => new { a.CompanyId, a.Status });
        builder.HasIndex(a => a.SerialNumber);
        builder.HasIndex(a => a.DepartmentId);
        builder.HasIndex(a => a.AssignedToUserId);
    }
}

