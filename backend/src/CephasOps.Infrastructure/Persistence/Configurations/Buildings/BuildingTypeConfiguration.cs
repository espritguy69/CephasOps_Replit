using CephasOps.Domain.Buildings.Entities;
using CephasOps.Domain.Departments.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Buildings;

public class BuildingTypeConfiguration : IEntityTypeConfiguration<BuildingType>
{
    public void Configure(EntityTypeBuilder<BuildingType> builder)
    {
        builder.ToTable("BuildingTypes");

        builder.HasKey(bt => bt.Id);

        builder.Property(bt => bt.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(bt => bt.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(bt => bt.Description)
            .HasMaxLength(500);

        // Optional department assignment
        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(bt => bt.DepartmentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(bt => new { bt.CompanyId, bt.DepartmentId });
        builder.HasIndex(bt => new { bt.CompanyId, bt.Code });
        builder.HasIndex(bt => new { bt.CompanyId, bt.IsActive });
        builder.HasIndex(bt => bt.DepartmentId);
    }
}

