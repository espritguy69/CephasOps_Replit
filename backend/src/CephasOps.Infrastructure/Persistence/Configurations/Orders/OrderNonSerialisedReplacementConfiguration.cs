using CephasOps.Domain.Orders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Orders;

public class OrderNonSerialisedReplacementConfiguration : IEntityTypeConfiguration<OrderNonSerialisedReplacement>
{
    public void Configure(EntityTypeBuilder<OrderNonSerialisedReplacement> builder)
    {
        builder.ToTable("OrderNonSerialisedReplacements");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.QuantityReplaced)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(r => r.Unit)
            .HasMaxLength(20);

        builder.Property(r => r.ReplacementReason)
            .HasMaxLength(500);

        builder.Property(r => r.Remark)
            .HasMaxLength(1000);

        // Indexes
        builder.HasIndex(r => new { r.CompanyId, r.OrderId });
        builder.HasIndex(r => r.MaterialId);

        // Relationship to Order
        builder.HasOne(r => r.Order)
            .WithMany(o => o.NonSerialisedReplacements)
            .HasForeignKey(r => r.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

