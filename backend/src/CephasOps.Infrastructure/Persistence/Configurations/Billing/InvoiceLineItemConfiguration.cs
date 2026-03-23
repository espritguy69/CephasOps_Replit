using CephasOps.Domain.Billing.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Billing;

public class InvoiceLineItemConfiguration : IEntityTypeConfiguration<InvoiceLineItem>
{
    public void Configure(EntityTypeBuilder<InvoiceLineItem> builder)
    {
        builder.ToTable("InvoiceLineItems");

        builder.HasKey(li => li.Id);

        builder.Property(li => li.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(li => li.Quantity)
            .HasPrecision(18, 3);

        builder.Property(li => li.UnitPrice)
            .HasPrecision(18, 2);

        builder.Property(li => li.Total)
            .HasPrecision(18, 2);

        builder.HasOne(li => li.Invoice)
            .WithMany(i => i.LineItems)
            .HasForeignKey(li => li.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(li => new { li.CompanyId, li.InvoiceId });
        builder.HasIndex(li => li.OrderId);
    }
}

