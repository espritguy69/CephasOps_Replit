using CephasOps.Domain.Buildings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Buildings;

public class BuildingContactConfiguration : IEntityTypeConfiguration<BuildingContact>
{
    public void Configure(EntityTypeBuilder<BuildingContact> builder)
    {
        builder.ToTable("BuildingContacts");

        builder.HasKey(bc => bc.Id);

        builder.Property(bc => bc.Role)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(bc => bc.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(bc => bc.Phone)
            .HasMaxLength(50);

        builder.Property(bc => bc.Email)
            .HasMaxLength(200);

        builder.Property(bc => bc.Remarks)
            .HasMaxLength(1000);

        // Relationship to Building (optional to avoid query filter warnings)
        builder.HasOne(bc => bc.Building)
            .WithMany()
            .HasForeignKey(bc => bc.BuildingId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(bc => bc.BuildingId);
        builder.HasIndex(bc => new { bc.BuildingId, bc.Role });
        builder.HasIndex(bc => new { bc.BuildingId, bc.IsActive });
    }
}

