using CephasOps.Domain.Buildings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Buildings;

public class PoleConfiguration : IEntityTypeConfiguration<Pole>
{
    public void Configure(EntityTypeBuilder<Pole> builder)
    {
        builder.ToTable("Poles");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.PoleNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.PoleType)
            .HasMaxLength(50);

        builder.Property(p => p.LocationDescription)
            .HasMaxLength(500);

        builder.Property(p => p.Remarks)
            .HasMaxLength(1000);

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(p => p.Latitude)
            .HasPrecision(10, 7);

        builder.Property(p => p.Longitude)
            .HasPrecision(10, 7);

        builder.Property(p => p.HeightMeters)
            .HasPrecision(5, 2);

        // Relationships (optional to avoid query filter warnings)
        builder.HasOne(p => p.Building)
            .WithMany()
            .HasForeignKey(p => p.BuildingId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Street)
            .WithMany()
            .HasForeignKey(p => p.StreetId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(p => p.BuildingId);
        builder.HasIndex(p => p.StreetId);
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => new { p.BuildingId, p.PoleNumber });
    }
}

