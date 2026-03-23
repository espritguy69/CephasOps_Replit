using CephasOps.Domain.Buildings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Buildings;

public class SplitterPortConfiguration : IEntityTypeConfiguration<SplitterPort>
{
    public void Configure(EntityTypeBuilder<SplitterPort> builder)
    {
        builder.ToTable("SplitterPorts");

        builder.HasKey(sp => sp.Id);

        builder.Property(sp => sp.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(sp => new { sp.CompanyId, sp.SplitterId, sp.PortNumber })
            .IsUnique();

        builder.HasIndex(sp => new { sp.CompanyId, sp.SplitterId, sp.Status });
        builder.HasIndex(sp => sp.OrderId);
    }
}

