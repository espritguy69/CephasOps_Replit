using CephasOps.Domain.Inventory.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Inventory;

public class SerialisedItemConfiguration : IEntityTypeConfiguration<SerialisedItem>
{
    public void Configure(EntityTypeBuilder<SerialisedItem> builder)
    {
        builder.ToTable("SerialisedItems");

        builder.HasKey(si => si.Id);

        builder.Property(si => si.SerialNumber)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(si => si.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(si => si.Notes)
            .HasMaxLength(1000);

        builder.HasOne(si => si.Material)
            .WithMany()
            .HasForeignKey(si => si.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(si => si.CurrentLocation)
            .WithMany()
            .HasForeignKey(si => si.CurrentLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(si => new { si.CompanyId, si.SerialNumber })
            .IsUnique();

        builder.HasIndex(si => new { si.CompanyId, si.MaterialId });
        builder.HasIndex(si => new { si.CompanyId, si.Status });
        builder.HasIndex(si => si.LastOrderId);
    }
}

