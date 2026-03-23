using CephasOps.Domain.Buildings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Buildings;

public class HubBoxConfiguration : IEntityTypeConfiguration<HubBox>
{
    public void Configure(EntityTypeBuilder<HubBox> builder)
    {
        builder.ToTable("HubBoxes");

        builder.HasKey(hb => hb.Id);

        builder.Property(hb => hb.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(hb => hb.Code)
            .HasMaxLength(50);

        builder.Property(hb => hb.LocationDescription)
            .HasMaxLength(500);

        builder.Property(hb => hb.Remarks)
            .HasMaxLength(1000);

        builder.Property(hb => hb.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(hb => hb.Latitude)
            .HasPrecision(10, 7);

        builder.Property(hb => hb.Longitude)
            .HasPrecision(10, 7);

        // Ignore computed properties
        builder.Ignore(hb => hb.PortsAvailable);
        builder.Ignore(hb => hb.IsFull);
        builder.Ignore(hb => hb.UtilizationPercent);

        // Relationships (optional to avoid query filter warnings)
        builder.HasOne(hb => hb.Building)
            .WithMany()
            .HasForeignKey(hb => hb.BuildingId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(hb => hb.Street)
            .WithMany()
            .HasForeignKey(hb => hb.StreetId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(hb => hb.BuildingId);
        builder.HasIndex(hb => hb.StreetId);
        builder.HasIndex(hb => hb.Status);
        builder.HasIndex(hb => new { hb.BuildingId, hb.Status });
    }
}

