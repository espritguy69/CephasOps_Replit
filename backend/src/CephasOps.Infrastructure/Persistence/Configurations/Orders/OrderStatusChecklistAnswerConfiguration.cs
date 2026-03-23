using CephasOps.Domain.Orders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Orders;

public class OrderStatusChecklistAnswerConfiguration : IEntityTypeConfiguration<OrderStatusChecklistAnswer>
{
    public void Configure(EntityTypeBuilder<OrderStatusChecklistAnswer> builder)
    {
        builder.ToTable("OrderStatusChecklistAnswers");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Remarks)
            .HasMaxLength(1000);

        // Foreign key to Order
        builder.HasOne(a => a.Order)
            .WithMany()
            .HasForeignKey(a => a.OrderId)
            .OnDelete(DeleteBehavior.Cascade); // Delete answers when order is deleted

        // Foreign key to ChecklistItem (configured in OrderStatusChecklistItemConfiguration)
        builder.HasOne(a => a.ChecklistItem)
            .WithMany(c => c.Answers)
            .HasForeignKey(a => a.ChecklistItemId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent deletion of item if answers exist

        // Indexes
        builder.HasIndex(a => new { a.CompanyId, a.OrderId });
        builder.HasIndex(a => new { a.OrderId, a.ChecklistItemId })
            .IsUnique(); // One answer per order per checklist item

        // Query filter for soft deletes
        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}

