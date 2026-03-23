using CephasOps.Domain.Orders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Orders;

public class OrderBlockerConfiguration : IEntityTypeConfiguration<OrderBlocker>
{
    public void Configure(EntityTypeBuilder<OrderBlocker> builder)
    {
        builder.ToTable("OrderBlockers");

        builder.HasKey(ob => ob.Id);

        builder.Property(ob => ob.BlockerType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ob => ob.BlockerCategory)
            .HasMaxLength(50);

        builder.Property(ob => ob.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(ob => ob.ResolutionNotes)
            .HasMaxLength(2000);

        // Evidence fields per ORDER_LIFECYCLE.md
        builder.Property(ob => ob.EvidenceAttachmentIds)
            .HasMaxLength(4000); // JSON array of Guids

        builder.Property(ob => ob.EvidenceRequired)
            .HasDefaultValue(true);

        builder.Property(ob => ob.EvidenceNotes)
            .HasMaxLength(2000);

        builder.HasIndex(ob => new { ob.CompanyId, ob.OrderId, ob.Resolved });
        builder.HasIndex(ob => ob.OrderId);
        builder.HasIndex(ob => ob.BlockerCategory);
    }
}

