using CephasOps.Domain.Orders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Orders;

public class OrderMaterialReplacementConfiguration : IEntityTypeConfiguration<OrderMaterialReplacement>
{
    public void Configure(EntityTypeBuilder<OrderMaterialReplacement> builder)
    {
        builder.ToTable("OrderMaterialReplacements");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.OldSerialNumber)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(r => r.NewSerialNumber)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(r => r.ApprovedBy)
            .HasMaxLength(200);

        builder.Property(r => r.ApprovalNotes)
            .HasMaxLength(1000);

        builder.Property(r => r.ReplacementReason)
            .HasMaxLength(500);

        builder.Property(r => r.Notes)
            .HasMaxLength(1000);

        // Indexes
        builder.HasIndex(r => new { r.CompanyId, r.OrderId });
        builder.HasIndex(r => r.OldMaterialId);
        builder.HasIndex(r => r.NewMaterialId);
        builder.HasIndex(r => r.OldSerialisedItemId);
        builder.HasIndex(r => r.NewSerialisedItemId);
        builder.HasIndex(r => r.RmaRequestId);

        // Relationship to Order
        builder.HasOne(r => r.Order)
            .WithMany(o => o.MaterialReplacements)
            .HasForeignKey(r => r.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

