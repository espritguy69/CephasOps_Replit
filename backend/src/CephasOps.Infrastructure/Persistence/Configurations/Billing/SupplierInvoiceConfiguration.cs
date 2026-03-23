using CephasOps.Domain.Billing.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Billing;

public class SupplierInvoiceConfiguration : IEntityTypeConfiguration<SupplierInvoice>
{
    public void Configure(EntityTypeBuilder<SupplierInvoice> builder)
    {
        builder.ToTable("SupplierInvoices");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.InvoiceNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(i => i.InternalReference)
            .HasMaxLength(50);

        builder.Property(i => i.SupplierName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.SupplierTaxNumber)
            .HasMaxLength(50);

        builder.Property(i => i.SupplierAddress)
            .HasMaxLength(500);

        builder.Property(i => i.SupplierEmail)
            .HasMaxLength(200);

        builder.Property(i => i.SubTotal)
            .HasPrecision(18, 2);

        builder.Property(i => i.TaxAmount)
            .HasPrecision(18, 2);

        builder.Property(i => i.TotalAmount)
            .HasPrecision(18, 2);

        builder.Property(i => i.AmountPaid)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(i => i.OutstandingAmount)
            .HasPrecision(18, 2);

        builder.Property(i => i.Currency)
            .HasMaxLength(3)
            .HasDefaultValue("MYR");

        builder.Property(i => i.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(i => i.Description)
            .HasMaxLength(500);

        builder.Property(i => i.Notes)
            .HasMaxLength(2000);

        builder.Property(i => i.AttachmentPath)
            .HasMaxLength(500);

        // Indexes
        builder.HasIndex(i => new { i.CompanyId, i.InvoiceNumber });
        builder.HasIndex(i => new { i.CompanyId, i.SupplierName });
        builder.HasIndex(i => new { i.CompanyId, i.Status });
        builder.HasIndex(i => i.InvoiceDate);
        builder.HasIndex(i => i.DueDate);
    }
}

