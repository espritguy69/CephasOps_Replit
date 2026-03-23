using CephasOps.Domain.Billing.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Billing;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.InvoiceNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(i => i.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(i => i.TermsInDays)
            .HasDefaultValue(45);

        builder.Property(i => i.SubmissionId)
            .HasMaxLength(200);

        builder.Property(i => i.TotalAmount)
            .HasPrecision(18, 2);

        builder.Property(i => i.TaxAmount)
            .HasPrecision(18, 2);

        builder.Property(i => i.SubTotal)
            .HasPrecision(18, 2);

        builder.HasMany(i => i.LineItems)
            .WithOne(li => li.Invoice)
            .HasForeignKey(li => li.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => new { i.CompanyId, i.InvoiceNumber })
            .IsUnique();

        builder.HasIndex(i => new { i.CompanyId, i.PartnerId });
        builder.HasIndex(i => new { i.CompanyId, i.Status });
        builder.HasIndex(i => new { i.CompanyId, i.InvoiceDate });
        builder.HasIndex(i => i.SubmissionId);

        // Concurrency token for optimistic concurrency control
        builder.Property(i => i.RowVersion)
            .IsRowVersion();
    }
}

