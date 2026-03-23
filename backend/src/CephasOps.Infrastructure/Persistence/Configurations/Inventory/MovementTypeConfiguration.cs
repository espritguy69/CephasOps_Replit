using CephasOps.Domain.Inventory.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Inventory;

public class MovementTypeConfiguration : IEntityTypeConfiguration<MovementType>
{
    public void Configure(EntityTypeBuilder<MovementType> builder)
    {
        builder.ToTable("MovementTypes");

        builder.HasKey(mt => mt.Id);

        builder.Property(mt => mt.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(mt => mt.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(mt => mt.Direction)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(mt => mt.StockImpact)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(mt => new { mt.CompanyId, mt.Code })
            .IsUnique();

        builder.HasIndex(mt => new { mt.CompanyId, mt.IsActive });
    }
}

