using CephasOps.Domain.Buildings.Entities;
using CephasOps.Domain.Departments.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Buildings;

public class BuildingConfiguration : IEntityTypeConfiguration<Building>
{
    public void Configure(EntityTypeBuilder<Building> builder)
    {
        builder.ToTable("Buildings");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(b => b.Code)
            .HasMaxLength(100);

        builder.Property(b => b.AddressLine1)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(b => b.AddressLine2)
            .HasMaxLength(500);

        builder.Property(b => b.City)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(b => b.State)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(b => b.Postcode)
            .IsRequired()
            .HasMaxLength(20);

        // Foreign key to BuildingType (nullable) - represents building classification
        builder.HasOne<BuildingType>()
            .WithMany()
            .HasForeignKey(b => b.BuildingTypeId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // Foreign key to InstallationMethod (nullable) - represents installation method
        builder.HasOne<InstallationMethod>()
            .WithMany()
            .HasForeignKey(b => b.InstallationMethodId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // Optional department assignment
        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(b => b.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(b => new { b.CompanyId, b.Name });
        builder.HasIndex(b => new { b.CompanyId, b.Code });
        builder.HasIndex(b => new { b.CompanyId, b.IsActive });
        builder.HasIndex(b => new { b.CompanyId, b.DepartmentId });
    }
}

