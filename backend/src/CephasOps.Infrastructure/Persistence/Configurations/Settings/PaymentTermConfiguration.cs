using CephasOps.Domain.Settings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Settings;

public class PaymentTermConfiguration : IEntityTypeConfiguration<PaymentTerm>
{
    public void Configure(EntityTypeBuilder<PaymentTerm> builder)
    {
        builder.ToTable("payment_terms");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.CompanyId)
            .HasColumnName("company_id");

        builder.Property(p => p.Code)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("code");

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("name");

        builder.Property(p => p.Description)
            .HasMaxLength(500)
            .HasColumnName("description");

        builder.Property(p => p.DueDays)
            .HasColumnName("due_days");

        builder.Property(p => p.DiscountPercent)
            .HasPrecision(5, 2)
            .HasColumnName("discount_percent");

        builder.Property(p => p.DiscountDays)
            .HasColumnName("discount_days");

        builder.Property(p => p.IsActive)
            .HasColumnName("is_active");

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(p => p.IsDeleted)
            .HasColumnName("is_deleted");

        builder.HasIndex(p => new { p.CompanyId, p.Code })
            .IsUnique()
            .HasFilter("is_deleted = false");

        builder.HasIndex(p => new { p.CompanyId, p.IsActive });
    }
}

