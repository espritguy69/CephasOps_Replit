using CephasOps.Domain.Procurement.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Procurement;

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("purchase_orders");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.PoNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.DeliveryAddress)
            .HasMaxLength(500);

        builder.Property(e => e.SubTotal)
            .HasPrecision(18, 2);

        builder.Property(e => e.TaxAmount)
            .HasPrecision(18, 2);

        builder.Property(e => e.DiscountAmount)
            .HasPrecision(18, 2);

        builder.Property(e => e.TotalAmount)
            .HasPrecision(18, 2);

        builder.Property(e => e.Currency)
            .HasMaxLength(10);

        builder.Property(e => e.PaymentTerms)
            .HasMaxLength(100);

        builder.Property(e => e.TermsAndConditions)
            .HasMaxLength(4000);

        builder.Property(e => e.Notes)
            .HasMaxLength(2000);

        builder.Property(e => e.InternalNotes)
            .HasMaxLength(2000);

        builder.HasIndex(e => new { e.CompanyId, e.PoNumber })
            .IsUnique();

        builder.HasIndex(e => new { e.CompanyId, e.SupplierId });
        builder.HasIndex(e => new { e.CompanyId, e.Status });
        builder.HasIndex(e => new { e.CompanyId, e.PoDate });

        builder.HasMany(e => e.Items)
            .WithOne(e => e.PurchaseOrder)
            .HasForeignKey(e => e.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PurchaseOrderItemConfiguration : IEntityTypeConfiguration<PurchaseOrderItem>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderItem> builder)
    {
        builder.ToTable("purchase_order_items");

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

        builder.Property(e => e.UnitPrice)
            .HasPrecision(18, 4);

        builder.Property(e => e.DiscountPercent)
            .HasPrecision(5, 2);

        builder.Property(e => e.TaxPercent)
            .HasPrecision(5, 2);

        builder.Property(e => e.Total)
            .HasPrecision(18, 2);

        builder.Property(e => e.QuantityReceived)
            .HasPrecision(18, 4);

        builder.Property(e => e.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(e => new { e.PurchaseOrderId, e.LineNumber });
    }
}

