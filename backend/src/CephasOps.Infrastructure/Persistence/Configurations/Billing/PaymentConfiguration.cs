using CephasOps.Domain.Billing.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Billing;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.PaymentNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.PaymentType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(p => p.PaymentMethod)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(p => p.Amount)
            .HasPrecision(18, 2);

        builder.Property(p => p.Currency)
            .HasMaxLength(3)
            .HasDefaultValue("MYR");

        builder.Property(p => p.PayerPayeeName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.BankAccount)
            .HasMaxLength(50);

        builder.Property(p => p.BankReference)
            .HasMaxLength(100);

        builder.Property(p => p.ChequeNumber)
            .HasMaxLength(50);

        builder.Property(p => p.Description)
            .HasMaxLength(500);

        builder.Property(p => p.Notes)
            .HasMaxLength(2000);

        builder.Property(p => p.AttachmentPath)
            .HasMaxLength(500);

        builder.Property(p => p.IsReconciled)
            .HasDefaultValue(false);

        builder.Property(p => p.IsVoided)
            .HasDefaultValue(false);

        builder.Property(p => p.VoidReason)
            .HasMaxLength(500);

        // Relationships
        builder.HasOne(p => p.Invoice)
            .WithMany()
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.SupplierInvoice)
            .WithMany(i => i.Payments)
            .HasForeignKey(p => p.SupplierInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(p => new { p.CompanyId, p.PaymentNumber }).IsUnique();
        builder.HasIndex(p => new { p.CompanyId, p.PaymentType });
        builder.HasIndex(p => new { p.CompanyId, p.PaymentDate });
        builder.HasIndex(p => p.InvoiceId);
        builder.HasIndex(p => p.SupplierInvoiceId);
        builder.HasIndex(p => p.IsReconciled);
        builder.HasIndex(p => p.IsVoided);
    }
}

