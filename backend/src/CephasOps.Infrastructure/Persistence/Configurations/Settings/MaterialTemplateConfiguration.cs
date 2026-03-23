using CephasOps.Domain.Settings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Settings;

public class MaterialTemplateConfiguration : IEntityTypeConfiguration<MaterialTemplate>
{
    public void Configure(EntityTypeBuilder<MaterialTemplate> builder)
    {
        builder.ToTable("MaterialTemplates");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.OrderType)
            .IsRequired()
            .HasMaxLength(100);

#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
        builder.HasIndex(m => new { m.CompanyId, m.OrderType, m.BuildingTypeId, m.PartnerId });
#pragma warning restore CS0618
        builder.HasIndex(m => new { m.CompanyId, m.IsDefault, m.OrderType });
        builder.HasIndex(m => new { m.CompanyId, m.IsActive });

        builder.HasMany(m => m.Items)
            .WithOne()
            .HasForeignKey("MaterialTemplateId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class MaterialTemplateItemConfiguration : IEntityTypeConfiguration<MaterialTemplateItem>
{
    public void Configure(EntityTypeBuilder<MaterialTemplateItem> builder)
    {
        builder.ToTable("MaterialTemplateItems");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.UnitOfMeasure)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(m => m.Notes)
            .HasMaxLength(500);

        builder.HasIndex(m => m.MaterialTemplateId);
        builder.HasIndex(m => m.MaterialId);
    }
}

