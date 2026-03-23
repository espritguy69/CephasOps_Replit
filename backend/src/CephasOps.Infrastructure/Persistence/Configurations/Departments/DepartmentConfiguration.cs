using CephasOps.Domain.Departments.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Departments;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(d => d.Code)
            .HasMaxLength(50);

        builder.Property(d => d.Description)
            .HasMaxLength(1000);

        builder.HasMany(d => d.MaterialAllocations)
            .WithOne(a => a.Department)
            .HasForeignKey(a => a.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => new { d.CompanyId, d.Name });
        builder.HasIndex(d => new { d.CompanyId, d.Code });
        builder.HasIndex(d => new { d.CompanyId, d.IsActive });
    }
}

