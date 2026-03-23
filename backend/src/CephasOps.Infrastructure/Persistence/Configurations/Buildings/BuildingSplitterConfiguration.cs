using CephasOps.Domain.Buildings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Buildings;

public class BuildingSplitterConfiguration : IEntityTypeConfiguration<BuildingSplitter>
{
    public void Configure(EntityTypeBuilder<BuildingSplitter> builder)
    {
        builder.ToTable("BuildingSplitters");

        builder.HasKey(bs => bs.Id);

        builder.Property(bs => bs.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(bs => bs.LocationDescription)
            .HasMaxLength(500);

        builder.Property(bs => bs.SerialNumber)
            .HasMaxLength(100);

        builder.Property(bs => bs.Remarks)
            .HasMaxLength(1000);

        builder.Property(bs => bs.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        // Ignore computed properties
        builder.Ignore(bs => bs.PortsAvailable);
        builder.Ignore(bs => bs.IsFull);
        builder.Ignore(bs => bs.UtilizationPercent);

        // Relationships (optional to avoid query filter warnings)
        builder.HasOne(bs => bs.Building)
            .WithMany()
            .HasForeignKey(bs => bs.BuildingId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(bs => bs.Block)
            .WithMany()
            .HasForeignKey(bs => bs.BlockId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(bs => bs.BuildingId);
        builder.HasIndex(bs => bs.BlockId);
        builder.HasIndex(bs => bs.SplitterTypeId);
        builder.HasIndex(bs => bs.Status);
        builder.HasIndex(bs => new { bs.BuildingId, bs.Status });
        builder.HasIndex(bs => new { bs.BuildingId, bs.NeedsAttention });
    }
}

