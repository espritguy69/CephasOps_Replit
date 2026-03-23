using CephasOps.Domain.Inventory.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Inventory;

public class StockLocationConfiguration : IEntityTypeConfiguration<StockLocation>
{
    public void Configure(EntityTypeBuilder<StockLocation> builder)
    {
        builder.ToTable("StockLocations");

        builder.HasKey(sl => sl.Id);

        builder.Property(sl => sl.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(sl => sl.Type)
            .HasMaxLength(50); // Made nullable for backward compatibility

        builder.Property(sl => sl.WarehouseId);

        builder.HasIndex(sl => new { sl.CompanyId, sl.Name });
        builder.HasIndex(sl => new { sl.CompanyId, sl.Type });
        builder.HasIndex(sl => new { sl.CompanyId, sl.LocationTypeId });
        builder.HasIndex(sl => sl.WarehouseId);

        // Foreign key relationships
        builder.HasOne(sl => sl.LocationType)
            .WithMany()
            .HasForeignKey(sl => sl.LocationTypeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

