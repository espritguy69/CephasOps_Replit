using CephasOps.Domain.Buildings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Buildings;

public class BuildingBlockConfiguration : IEntityTypeConfiguration<BuildingBlock>
{
    public void Configure(EntityTypeBuilder<BuildingBlock> builder)
    {
        builder.ToTable("BuildingBlocks");

        builder.HasKey(bb => bb.Id);

        builder.Property(bb => bb.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(bb => bb.Code)
            .HasMaxLength(50);

        builder.Property(bb => bb.Notes)
            .HasMaxLength(1000);

        // Relationship to Building (optional to avoid query filter warnings)
        builder.HasOne(bb => bb.Building)
            .WithMany()
            .HasForeignKey(bb => bb.BuildingId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(bb => bb.BuildingId);
        builder.HasIndex(bb => new { bb.BuildingId, bb.Name });
        builder.HasIndex(bb => new { bb.BuildingId, bb.DisplayOrder });
    }
}

