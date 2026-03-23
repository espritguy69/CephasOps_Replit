using CephasOps.Domain.Buildings.Entities;
using CephasOps.Domain.Departments.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Buildings;

public class SplitterTypeConfiguration : IEntityTypeConfiguration<SplitterType>
{
    public void Configure(EntityTypeBuilder<SplitterType> builder)
    {
        builder.ToTable("SplitterTypes");

        builder.HasKey(st => st.Id);

        builder.Property(st => st.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(st => st.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(st => st.Description)
            .HasMaxLength(500);

        builder.Property(st => st.TotalPorts)
            .IsRequired();

        // Optional department assignment
        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(st => st.DepartmentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(st => new { st.CompanyId, st.DepartmentId });
        builder.HasIndex(st => new { st.CompanyId, st.Code });
        builder.HasIndex(st => new { st.CompanyId, st.IsActive });
        builder.HasIndex(st => st.DepartmentId);
        
        // Unique constraint: Code must be unique per company (or globally if CompanyId is null)
        // Note: EF Core doesn't support COALESCE in unique constraints directly
        // The unique constraint will be added via migration SQL
        // For now, we rely on application-level validation and the migration script
    }
}

