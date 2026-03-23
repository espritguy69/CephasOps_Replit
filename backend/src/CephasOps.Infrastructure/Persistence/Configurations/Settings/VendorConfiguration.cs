using CephasOps.Domain.Settings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Settings;

public class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    public void Configure(EntityTypeBuilder<Vendor> builder)
    {
        builder.ToTable("vendors");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.CompanyId)
            .HasColumnName("company_id");

        builder.Property(v => v.Code)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("code");

        builder.Property(v => v.Name)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("name");

        builder.Property(v => v.Description)
            .HasMaxLength(500)
            .HasColumnName("description");

        builder.Property(v => v.ContactPerson)
            .HasMaxLength(200)
            .HasColumnName("contact_person");

        builder.Property(v => v.ContactPhone)
            .HasMaxLength(50)
            .HasColumnName("contact_phone");

        builder.Property(v => v.ContactEmail)
            .HasMaxLength(200)
            .HasColumnName("contact_email");

        builder.Property(v => v.Address)
            .HasMaxLength(500)
            .HasColumnName("address");

        builder.Property(v => v.City)
            .HasMaxLength(100)
            .HasColumnName("city");

        builder.Property(v => v.State)
            .HasMaxLength(100)
            .HasColumnName("state");

        builder.Property(v => v.PostCode)
            .HasMaxLength(20)
            .HasColumnName("post_code");

        builder.Property(v => v.Country)
            .HasMaxLength(100)
            .HasColumnName("country");

        builder.Property(v => v.PaymentTerms)
            .HasMaxLength(100)
            .HasColumnName("payment_terms");

        builder.Property(v => v.PaymentDueDays)
            .HasColumnName("payment_due_days");

        builder.Property(v => v.IsActive)
            .HasColumnName("is_active");

        builder.Property(v => v.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(v => v.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(v => v.CreatedBy)
            .HasMaxLength(200)
            .HasColumnName("created_by");

        builder.Property(v => v.UpdatedBy)
            .HasMaxLength(200)
            .HasColumnName("updated_by");

        builder.Property(v => v.IsDeleted)
            .HasColumnName("is_deleted");

        builder.Property(v => v.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasIndex(v => new { v.CompanyId, v.Code })
            .IsUnique()
            .HasFilter("is_deleted = false");

        builder.HasIndex(v => new { v.CompanyId, v.IsActive });
    }
}

