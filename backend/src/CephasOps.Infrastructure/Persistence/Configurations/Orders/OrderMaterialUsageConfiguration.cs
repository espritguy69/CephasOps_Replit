using CephasOps.Domain.Orders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Orders;

public class OrderMaterialUsageConfiguration : IEntityTypeConfiguration<OrderMaterialUsage>
{
    public void Configure(EntityTypeBuilder<OrderMaterialUsage> builder)
    {
        builder.ToTable("OrderMaterialUsage");

        builder.HasKey(omu => omu.Id);

        builder.Property(omu => omu.Quantity)
            .HasPrecision(18, 4);

        builder.Property(omu => omu.UnitCost)
            .HasPrecision(18, 4);

        builder.Property(omu => omu.TotalCost)
            .HasPrecision(18, 4);

        builder.Property(omu => omu.Notes)
            .HasMaxLength(2000);

        builder.HasIndex(omu => new { omu.CompanyId, omu.OrderId });
        builder.HasIndex(omu => new { omu.CompanyId, omu.MaterialId });
        builder.HasIndex(omu => omu.SerialisedItemId);

        // Navigation properties
        builder.HasOne(omu => omu.Material)
            .WithMany()
            .HasForeignKey(omu => omu.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(omu => omu.SerialisedItem)
            .WithMany()
            .HasForeignKey(omu => omu.SerialisedItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

