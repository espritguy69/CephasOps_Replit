using CephasOps.Domain.Buildings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Buildings;

public class StreetConfiguration : IEntityTypeConfiguration<Street>
{
    public void Configure(EntityTypeBuilder<Street> builder)
    {
        builder.ToTable("Streets");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Code)
            .HasMaxLength(50);

        builder.Property(s => s.Section)
            .HasMaxLength(100);

        // Relationship to Building (optional to avoid query filter warnings)
        builder.HasOne(s => s.Building)
            .WithMany()
            .HasForeignKey(s => s.BuildingId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(s => s.BuildingId);
        builder.HasIndex(s => new { s.BuildingId, s.Name });
        builder.HasIndex(s => new { s.BuildingId, s.DisplayOrder });
    }
}

