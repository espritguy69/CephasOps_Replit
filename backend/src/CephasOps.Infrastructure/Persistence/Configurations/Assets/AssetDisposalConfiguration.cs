using CephasOps.Domain.Assets.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Assets;

public class AssetDisposalConfiguration : IEntityTypeConfiguration<AssetDisposal>
{
    public void Configure(EntityTypeBuilder<AssetDisposal> builder)
    {
        builder.ToTable("AssetDisposals");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.DisposalMethod)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(d => d.BookValueAtDisposal)
            .HasPrecision(18, 2);

        builder.Property(d => d.DisposalProceeds)
            .HasPrecision(18, 2);

        builder.Property(d => d.GainLoss)
            .HasPrecision(18, 2);

        builder.Property(d => d.BuyerName)
            .HasMaxLength(200);

        builder.Property(d => d.ReferenceNumber)
            .HasMaxLength(100);

        builder.Property(d => d.Reason)
            .HasMaxLength(500);

        builder.Property(d => d.Notes)
            .HasMaxLength(2000);

        builder.Property(d => d.IsApproved)
            .HasDefaultValue(false);

        // Indexes
        builder.HasIndex(d => new { d.CompanyId, d.AssetId }).IsUnique();
        builder.HasIndex(d => d.DisposalDate);
        builder.HasIndex(d => d.IsApproved);
    }
}

