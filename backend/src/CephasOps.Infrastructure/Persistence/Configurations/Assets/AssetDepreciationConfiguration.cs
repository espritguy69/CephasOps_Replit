using CephasOps.Domain.Assets.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Assets;

public class AssetDepreciationConfiguration : IEntityTypeConfiguration<AssetDepreciation>
{
    public void Configure(EntityTypeBuilder<AssetDepreciation> builder)
    {
        builder.ToTable("AssetDepreciationEntries");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Period)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(d => d.DepreciationAmount)
            .HasPrecision(18, 2);

        builder.Property(d => d.OpeningBookValue)
            .HasPrecision(18, 2);

        builder.Property(d => d.ClosingBookValue)
            .HasPrecision(18, 2);

        builder.Property(d => d.AccumulatedDepreciation)
            .HasPrecision(18, 2);

        builder.Property(d => d.IsPosted)
            .HasDefaultValue(false);

        builder.Property(d => d.Notes)
            .HasMaxLength(500);

        // Relationships
        builder.HasOne(d => d.Asset)
            .WithMany(a => a.DepreciationEntries)
            .HasForeignKey(d => d.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(d => new { d.CompanyId, d.AssetId, d.Period }).IsUnique();
        builder.HasIndex(d => new { d.CompanyId, d.Period });
        builder.HasIndex(d => d.IsPosted);
    }
}

