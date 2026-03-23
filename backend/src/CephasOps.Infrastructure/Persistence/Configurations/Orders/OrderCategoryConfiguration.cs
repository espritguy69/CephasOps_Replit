using CephasOps.Domain.Orders.Entities;
using CephasOps.Domain.Departments.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Orders;

public class OrderCategoryConfiguration : IEntityTypeConfiguration<OrderCategory>
{
    public void Configure(EntityTypeBuilder<OrderCategory> builder)
    {
        builder.ToTable("OrderCategories");

        builder.HasKey(oc => oc.Id);

        builder.Property(oc => oc.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(oc => oc.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(oc => oc.Description)
            .HasMaxLength(500);

        // Optional department assignment
        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(oc => oc.DepartmentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(oc => new { oc.CompanyId, oc.DepartmentId });
        builder.HasIndex(oc => new { oc.CompanyId, oc.Code });
        builder.HasIndex(oc => new { oc.CompanyId, oc.IsActive });
        builder.HasIndex(oc => oc.DepartmentId);
    }
}

