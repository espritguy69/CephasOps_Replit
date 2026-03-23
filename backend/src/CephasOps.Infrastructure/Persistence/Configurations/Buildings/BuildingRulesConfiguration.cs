using CephasOps.Domain.Buildings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Buildings;

public class BuildingRulesConfiguration : IEntityTypeConfiguration<BuildingRules>
{
    public void Configure(EntityTypeBuilder<BuildingRules> builder)
    {
        builder.ToTable("BuildingRules");

        builder.HasKey(br => br.Id);

        builder.Property(br => br.AccessRules)
            .HasColumnType("text");

        builder.Property(br => br.InstallationRules)
            .HasColumnType("text");

        builder.Property(br => br.OtherNotes)
            .HasColumnType("text");

        // One-to-one relationship with Building (optional to avoid query filter warnings)
        builder.HasOne(br => br.Building)
            .WithOne()
            .HasForeignKey<BuildingRules>(br => br.BuildingId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint on BuildingId (1:1)
        builder.HasIndex(br => br.BuildingId).IsUnique();
    }
}

