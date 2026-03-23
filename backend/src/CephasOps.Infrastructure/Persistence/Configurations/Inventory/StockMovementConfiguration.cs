using CephasOps.Domain.Inventory.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Inventory;

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements");

        builder.HasKey(sm => sm.Id);

        builder.Property(sm => sm.MovementType)
            .HasMaxLength(50); // Made nullable for backward compatibility

        builder.Property(sm => sm.MovementTypeId);

        // Foreign key relationship
        builder.HasOne(sm => sm.MovementTypeNavigation)
            .WithMany()
            .HasForeignKey(sm => sm.MovementTypeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(sm => sm.Remarks)
            .HasMaxLength(1000);

        builder.HasOne(sm => sm.Material)
            .WithMany()
            .HasForeignKey(sm => sm.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sm => sm.FromLocation)
            .WithMany()
            .HasForeignKey(sm => sm.FromLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sm => sm.ToLocation)
            .WithMany()
            .HasForeignKey(sm => sm.ToLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(sm => new { sm.CompanyId, sm.MaterialId });
        builder.HasIndex(sm => new { sm.CompanyId, sm.OrderId });
        builder.HasIndex(sm => new { sm.CompanyId, sm.CreatedAt });
        builder.HasIndex(sm => sm.MovementType);
    }
}

