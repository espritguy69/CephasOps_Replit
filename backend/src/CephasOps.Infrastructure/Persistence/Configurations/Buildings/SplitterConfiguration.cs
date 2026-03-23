using CephasOps.Domain.Buildings.Entities;
using CephasOps.Domain.Departments.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Buildings;

public class SplitterConfiguration : IEntityTypeConfiguration<Splitter>
{
    public void Configure(EntityTypeBuilder<Splitter> builder)
    {
        builder.ToTable("Splitters");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Code)
            .HasMaxLength(100);

        // Foreign key to SplitterType (nullable for backward compatibility)
        builder.HasOne<SplitterType>()
            .WithMany()
            .HasForeignKey(s => s.SplitterTypeId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(s => s.Location)
            .HasMaxLength(200);

        builder.Property(s => s.Block)
            .HasMaxLength(50);

        builder.Property(s => s.Floor)
            .HasMaxLength(50);

        // Optional department assignment
        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(s => s.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(s => new { s.CompanyId, s.BuildingId });
        builder.HasIndex(s => new { s.CompanyId, s.IsActive });
        builder.HasIndex(s => new { s.CompanyId, s.DepartmentId });

        // Concurrency token for optimistic concurrency control
        builder.Property(s => s.RowVersion)
            .IsRowVersion();
    }
}

