using CephasOps.Domain.Inventory.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Inventory;

public class DeliveryOrderConfiguration : IEntityTypeConfiguration<DeliveryOrder>
{
    public void Configure(EntityTypeBuilder<DeliveryOrder> builder)
    {
        builder.ToTable("delivery_orders");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.DoNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.DoType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.RecipientName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.RecipientPhone)
            .HasMaxLength(50);

        builder.Property(e => e.RecipientEmail)
            .HasMaxLength(200);

        builder.Property(e => e.DeliveryAddress)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.City)
            .HasMaxLength(100);

        builder.Property(e => e.State)
            .HasMaxLength(100);

        builder.Property(e => e.Postcode)
            .HasMaxLength(20);

        builder.Property(e => e.DeliveryPerson)
            .HasMaxLength(100);

        builder.Property(e => e.VehicleNumber)
            .HasMaxLength(50);

        builder.Property(e => e.Notes)
            .HasMaxLength(2000);

        builder.Property(e => e.InternalNotes)
            .HasMaxLength(2000);

        builder.Property(e => e.ReceivedByName)
            .HasMaxLength(200);

        builder.HasIndex(e => new { e.CompanyId, e.DoNumber })
            .IsUnique();

        builder.HasIndex(e => new { e.CompanyId, e.Status });
        builder.HasIndex(e => new { e.CompanyId, e.DoDate });
        builder.HasIndex(e => new { e.CompanyId, e.DoType });

        builder.HasMany(e => e.Items)
            .WithOne(e => e.DeliveryOrder)
            .HasForeignKey(e => e.DeliveryOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DeliveryOrderItemConfiguration : IEntityTypeConfiguration<DeliveryOrderItem>
{
    public void Configure(EntityTypeBuilder<DeliveryOrderItem> builder)
    {
        builder.ToTable("delivery_order_items");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.Sku)
            .HasMaxLength(100);

        builder.Property(e => e.Unit)
            .HasMaxLength(20);

        builder.Property(e => e.Quantity)
            .HasPrecision(18, 4);

        builder.Property(e => e.QuantityDelivered)
            .HasPrecision(18, 4);

        builder.Property(e => e.SerialNumbers)
            .HasMaxLength(4000);

        builder.Property(e => e.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(e => new { e.DeliveryOrderId, e.LineNumber });

        builder.HasOne(e => e.Material)
            .WithMany()
            .HasForeignKey(e => e.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

