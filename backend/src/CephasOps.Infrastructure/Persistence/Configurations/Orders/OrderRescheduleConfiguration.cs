using CephasOps.Domain.Orders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Orders;

public class OrderRescheduleConfiguration : IEntityTypeConfiguration<OrderReschedule>
{
    public void Configure(EntityTypeBuilder<OrderReschedule> builder)
    {
        builder.ToTable("OrderReschedules");

        builder.HasKey(or => or.Id);

        builder.Property(or => or.RequestedBySource)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(or => or.Reason)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(or => or.ApprovalSource)
            .HasMaxLength(50);

        builder.Property(or => or.Status)
            .IsRequired()
            .HasMaxLength(50);

        // Same-day reschedule evidence fields per ORDER_LIFECYCLE.md section 6.2
        builder.Property(or => or.IsSameDayReschedule)
            .HasDefaultValue(false);

        builder.Property(or => or.SameDayEvidenceNotes)
            .HasMaxLength(2000);

        builder.HasIndex(or => new { or.CompanyId, or.OrderId });
        builder.HasIndex(or => new { or.CompanyId, or.Status });
        builder.HasIndex(or => or.IsSameDayReschedule);
    }
}

