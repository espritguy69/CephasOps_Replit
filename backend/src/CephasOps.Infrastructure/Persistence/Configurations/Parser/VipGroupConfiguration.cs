using CephasOps.Domain.Parser.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Parser;

public class VipGroupConfiguration : IEntityTypeConfiguration<VipGroup>
{
    public void Configure(EntityTypeBuilder<VipGroup> builder)
    {
        builder.ToTable("VipGroups");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(v => v.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(v => v.Description)
            .HasMaxLength(1000);

        builder.Property(v => v.NotifyRole)
            .HasMaxLength(100);

        builder.HasIndex(v => new { v.CompanyId, v.Code })
            .IsUnique();

        builder.HasIndex(v => new { v.CompanyId, v.IsActive });
    }
}

