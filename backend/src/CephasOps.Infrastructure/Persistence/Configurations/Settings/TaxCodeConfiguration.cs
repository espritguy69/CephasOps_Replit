using CephasOps.Domain.Settings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Settings;

public class TaxCodeConfiguration : IEntityTypeConfiguration<TaxCode>
{
    public void Configure(EntityTypeBuilder<TaxCode> builder)
    {
        builder.ToTable("tax_codes");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.CompanyId)
            .HasColumnName("company_id");

        builder.Property(t => t.Code)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("code");

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("name");

        builder.Property(t => t.Description)
            .HasMaxLength(500)
            .HasColumnName("description");

        builder.Property(t => t.TaxRate)
            .HasPrecision(5, 2)
            .HasColumnName("tax_rate");

        builder.Property(t => t.IsDefault)
            .HasColumnName("is_default");

        builder.Property(t => t.IsActive)
            .HasColumnName("is_active");

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(t => t.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(t => t.IsDeleted)
            .HasColumnName("is_deleted");

        builder.HasIndex(t => new { t.CompanyId, t.Code })
            .IsUnique()
            .HasFilter("is_deleted = false");

        builder.HasIndex(t => new { t.CompanyId, t.IsActive, t.IsDefault });
    }
}

