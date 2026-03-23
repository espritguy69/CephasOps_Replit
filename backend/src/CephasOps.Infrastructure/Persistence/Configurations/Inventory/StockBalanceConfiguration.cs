using CephasOps.Domain.Inventory.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Inventory;

public class StockBalanceConfiguration : IEntityTypeConfiguration<StockBalance>
{
    public void Configure(EntityTypeBuilder<StockBalance> builder)
    {
        builder.ToTable("StockBalances");

        builder.HasKey(sb => sb.Id);

        builder.HasOne(sb => sb.Material)
            .WithMany()
            .HasForeignKey(sb => sb.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sb => sb.StockLocation)
            .WithMany()
            .HasForeignKey(sb => sb.StockLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(sb => new { sb.CompanyId, sb.MaterialId, sb.StockLocationId })
            .IsUnique();

        builder.HasIndex(sb => new { sb.CompanyId, sb.StockLocationId });
    }
}

