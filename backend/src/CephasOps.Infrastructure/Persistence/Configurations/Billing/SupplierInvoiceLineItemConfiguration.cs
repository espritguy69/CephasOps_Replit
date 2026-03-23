using CephasOps.Domain.Billing.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Billing;

public class SupplierInvoiceLineItemConfiguration : IEntityTypeConfiguration<SupplierInvoiceLineItem>
{
    public void Configure(EntityTypeBuilder<SupplierInvoiceLineItem> builder)
    {
        builder.ToTable("SupplierInvoiceLineItems");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.LineNumber)
            .IsRequired();

        builder.Property(l => l.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(l => l.Quantity)
            .HasPrecision(18, 4)
            .HasDefaultValue(1);

        builder.Property(l => l.UnitOfMeasure)
            .HasMaxLength(20);

        builder.Property(l => l.UnitPrice)
            .HasPrecision(18, 4);

        builder.Property(l => l.LineTotal)
            .HasPrecision(18, 2);

        builder.Property(l => l.TaxRate)
            .HasPrecision(5, 2)
            .HasDefaultValue(0);

        builder.Property(l => l.TaxAmount)
            .HasPrecision(18, 2);

        builder.Property(l => l.TotalWithTax)
            .HasPrecision(18, 2);

        builder.Property(l => l.Notes)
            .HasMaxLength(500);

        // Relationships
        builder.HasOne(l => l.SupplierInvoice)
            .WithMany(i => i.LineItems)
            .HasForeignKey(l => l.SupplierInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(l => new { l.SupplierInvoiceId, l.LineNumber });
        builder.HasIndex(l => l.PnlTypeId);
        builder.HasIndex(l => l.AssetId);
    }
}

