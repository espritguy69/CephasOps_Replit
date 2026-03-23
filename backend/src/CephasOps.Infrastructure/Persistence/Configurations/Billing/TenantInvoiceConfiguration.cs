using CephasOps.Domain.Billing.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Billing;

public class TenantInvoiceConfiguration : IEntityTypeConfiguration<TenantInvoice>
{
    public void Configure(EntityTypeBuilder<TenantInvoice> builder)
    {
        builder.ToTable("TenantInvoices");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.InvoiceNumber).IsRequired().HasMaxLength(64);
        builder.Property(i => i.Currency).HasMaxLength(3);
        builder.Property(i => i.Status).HasMaxLength(32);
        builder.Property(i => i.SubTotal).HasPrecision(18, 2);
        builder.Property(i => i.TaxAmount).HasPrecision(18, 2);
        builder.Property(i => i.TotalAmount).HasPrecision(18, 2);
        builder.HasIndex(i => i.TenantId);
        builder.HasIndex(i => new { i.TenantId, i.InvoiceNumber }).IsUnique();
    }
}
