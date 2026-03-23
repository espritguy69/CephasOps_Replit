using CephasOps.Domain.Assets.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Assets;

public class AssetTypeConfiguration : IEntityTypeConfiguration<AssetType>
{
    public void Configure(EntityTypeBuilder<AssetType> builder)
    {
        builder.ToTable("AssetTypes");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(t => t.Description)
            .HasMaxLength(500);

        builder.Property(t => t.DefaultDepreciationMethod)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(t => t.DefaultUsefulLifeMonths)
            .HasDefaultValue(60);

        builder.Property(t => t.DefaultSalvageValuePercent)
            .HasPrecision(5, 2)
            .HasDefaultValue(10);

        builder.Property(t => t.IsActive)
            .HasDefaultValue(true);

        builder.Property(t => t.SortOrder)
            .HasDefaultValue(0);

        builder.HasIndex(t => new { t.CompanyId, t.Code }).IsUnique();
        builder.HasIndex(t => t.IsActive);
    }
}

