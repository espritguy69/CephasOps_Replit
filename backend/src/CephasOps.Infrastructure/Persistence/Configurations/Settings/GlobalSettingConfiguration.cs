using CephasOps.Domain.Settings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Settings;

public class GlobalSettingConfiguration : IEntityTypeConfiguration<GlobalSetting>
{
    public void Configure(EntityTypeBuilder<GlobalSetting> builder)
    {
        builder.ToTable("GlobalSettings");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Key)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(g => g.Value)
            .IsRequired()
            .HasMaxLength(5000);

        builder.Property(g => g.ValueType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(g => g.Description)
            .HasMaxLength(1000);

        builder.Property(g => g.Module)
            .HasMaxLength(100);

        builder.HasIndex(g => g.Key)
            .IsUnique();

        builder.HasIndex(g => g.Module);
    }
}

