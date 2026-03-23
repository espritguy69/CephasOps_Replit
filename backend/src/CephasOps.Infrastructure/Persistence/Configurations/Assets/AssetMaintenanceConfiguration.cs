using CephasOps.Domain.Assets.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Assets;

public class AssetMaintenanceConfiguration : IEntityTypeConfiguration<AssetMaintenance>
{
    public void Configure(EntityTypeBuilder<AssetMaintenance> builder)
    {
        builder.ToTable("AssetMaintenanceRecords");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.MaintenanceType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(m => m.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(m => m.Cost)
            .HasPrecision(18, 2);

        builder.Property(m => m.PerformedBy)
            .HasMaxLength(200);

        builder.Property(m => m.ReferenceNumber)
            .HasMaxLength(100);

        builder.Property(m => m.Notes)
            .HasMaxLength(2000);

        builder.Property(m => m.IsCompleted)
            .HasDefaultValue(false);

        // Relationships
        builder.HasOne(m => m.Asset)
            .WithMany(a => a.MaintenanceRecords)
            .HasForeignKey(m => m.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(m => new { m.CompanyId, m.AssetId });
        builder.HasIndex(m => m.ScheduledDate);
        builder.HasIndex(m => m.PerformedDate);
        builder.HasIndex(m => m.NextScheduledDate);
        builder.HasIndex(m => m.IsCompleted);
    }
}

