using CephasOps.Domain.Departments.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Departments;

public class MaterialAllocationConfiguration : IEntityTypeConfiguration<MaterialAllocation>
{
    public void Configure(EntityTypeBuilder<MaterialAllocation> builder)
    {
        builder.ToTable("MaterialAllocations");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Quantity)
            .HasPrecision(18, 3);

        builder.Property(a => a.Notes)
            .HasMaxLength(1000);

        builder.HasOne(a => a.Department)
            .WithMany(d => d.MaterialAllocations)
            .HasForeignKey(a => a.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => new { a.CompanyId, a.DepartmentId });
        builder.HasIndex(a => a.MaterialId);
    }
}

