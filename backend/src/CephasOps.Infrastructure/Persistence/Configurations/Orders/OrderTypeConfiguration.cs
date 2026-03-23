using CephasOps.Domain.Orders.Entities;
using CephasOps.Domain.Departments.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Orders;

public class OrderTypeConfiguration : IEntityTypeConfiguration<OrderType>
{
    public void Configure(EntityTypeBuilder<OrderType> builder)
    {
        builder.ToTable("OrderTypes");

        builder.HasKey(ot => ot.Id);

        builder.Property(ot => ot.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(ot => ot.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ot => ot.Description)
            .HasMaxLength(500);

        // Optional department assignment
        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(ot => ot.DepartmentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(ot => new { ot.CompanyId, ot.DepartmentId });
        builder.HasIndex(ot => new { ot.CompanyId, ot.IsActive });
        builder.HasIndex(ot => ot.DepartmentId);

        // Uniqueness: one parent per (CompanyId, Code); one subtype per (CompanyId, ParentOrderTypeId, Code). Replaces previous non-unique (CompanyId, Code).
        builder.HasIndex(ot => new { ot.CompanyId, ot.Code })
            .IsUnique()
            .HasFilter("\"ParentOrderTypeId\" IS NULL")
            .HasDatabaseName("IX_OrderTypes_CompanyId_Code_Parents");
        builder.HasIndex(ot => new { ot.CompanyId, ot.ParentOrderTypeId, ot.Code })
            .IsUnique()
            .HasFilter("\"ParentOrderTypeId\" IS NOT NULL")
            .HasDatabaseName("IX_OrderTypes_CompanyId_ParentId_Code_Subtypes");
    }
}

