using CephasOps.Domain.Orders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Orders;

public class OrderStatusChecklistItemConfiguration : IEntityTypeConfiguration<OrderStatusChecklistItem>
{
    public void Configure(EntityTypeBuilder<OrderStatusChecklistItem> builder)
    {
        builder.ToTable("OrderStatusChecklistItems");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.StatusCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Description)
            .HasMaxLength(1000);

        // Self-referencing relationship for parent-child
        builder.HasOne(c => c.Parent)
            .WithMany(c => c.SubSteps)
            .HasForeignKey(c => c.ParentChecklistItemId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

        // Navigation to answers
        builder.HasMany(c => c.Answers)
            .WithOne(a => a.ChecklistItem)
            .HasForeignKey(a => a.ChecklistItemId)
            .OnDelete(DeleteBehavior.Cascade); // Delete answers when item is deleted

        // Indexes
        builder.HasIndex(c => new { c.CompanyId, c.StatusCode, c.IsActive });
        builder.HasIndex(c => new { c.StatusCode, c.OrderIndex });
        builder.HasIndex(c => c.ParentChecklistItemId);

        // Query filter for soft deletes
        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}

