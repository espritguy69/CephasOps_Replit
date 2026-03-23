using CephasOps.Domain.Inventory.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Inventory;

public class LocationTypeConfiguration : IEntityTypeConfiguration<LocationType>
{
    public void Configure(EntityTypeBuilder<LocationType> builder)
    {
        builder.ToTable("LocationTypes");

        builder.HasKey(lt => lt.Id);

        builder.Property(lt => lt.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(lt => lt.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(lt => lt.AutoCreateTrigger)
            .HasMaxLength(50);

        builder.HasIndex(lt => new { lt.CompanyId, lt.Code })
            .IsUnique();

        builder.HasIndex(lt => new { lt.CompanyId, lt.IsActive });
    }
}

