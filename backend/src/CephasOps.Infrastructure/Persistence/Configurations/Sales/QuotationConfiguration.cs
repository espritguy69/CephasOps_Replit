using CephasOps.Domain.Sales.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Sales;

public class QuotationConfiguration : IEntityTypeConfiguration<Quotation>
{
    public void Configure(EntityTypeBuilder<Quotation> builder)
    {
        builder.ToTable("quotations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.QuotationNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.CustomerName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.CustomerPhone)
            .HasMaxLength(50);

        builder.Property(e => e.CustomerEmail)
            .HasMaxLength(200);

        builder.Property(e => e.CustomerAddress)
            .HasMaxLength(500);

        builder.Property(e => e.Status)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Subject)
            .HasMaxLength(200);

        builder.Property(e => e.Introduction)
            .HasMaxLength(4000);

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
            .HasMaxLength(200);

        builder.Property(e => e.DeliveryTerms)
            .HasMaxLength(200);

        builder.Property(e => e.TermsAndConditions)
            .HasMaxLength(4000);

        builder.Property(e => e.Notes)
            .HasMaxLength(2000);

        builder.Property(e => e.InternalNotes)
            .HasMaxLength(2000);

        builder.Property(e => e.RejectionReason)
            .HasMaxLength(1000);

        builder.HasIndex(e => new { e.CompanyId, e.QuotationNumber })
            .IsUnique();

        builder.HasIndex(e => new { e.CompanyId, e.Status });
        builder.HasIndex(e => new { e.CompanyId, e.QuotationDate });
        builder.HasIndex(e => new { e.CompanyId, e.PartnerId });

        builder.HasMany(e => e.Items)
            .WithOne(e => e.Quotation)
            .HasForeignKey(e => e.QuotationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class QuotationItemConfiguration : IEntityTypeConfiguration<QuotationItem>
{
    public void Configure(EntityTypeBuilder<QuotationItem> builder)
    {
        builder.ToTable("quotation_items");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ItemType)
            .HasMaxLength(50);

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

        builder.Property(e => e.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(e => new { e.QuotationId, e.LineNumber });
    }
}

