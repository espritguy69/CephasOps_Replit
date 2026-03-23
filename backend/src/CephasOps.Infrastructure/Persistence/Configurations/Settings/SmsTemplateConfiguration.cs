using CephasOps.Domain.Settings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Settings;

public class SmsTemplateConfiguration : IEntityTypeConfiguration<SmsTemplate>
{
    public void Configure(EntityTypeBuilder<SmsTemplate> builder)
    {
        builder.ToTable("sms_templates");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Code)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("code");

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("name");

        builder.Property(s => s.Description)
            .HasMaxLength(500)
            .HasColumnName("description");

        builder.Property(s => s.Category)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("category");

        builder.Property(s => s.MessageText)
            .IsRequired()
            .HasMaxLength(1000)
            .HasColumnName("message_text");

        builder.Property(s => s.CharCount)
            .HasColumnName("char_count");

        builder.Property(s => s.IsActive)
            .HasColumnName("is_active");

        builder.Property(s => s.CreatedByUserId)
            .HasColumnName("created_by_user_id");

        builder.Property(s => s.UpdatedByUserId)
            .HasColumnName("updated_by_user_id");

        builder.Property(s => s.Notes)
            .HasMaxLength(1000)
            .HasColumnName("notes");

        builder.Property(s => s.CompanyId)
            .HasColumnName("company_id");

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(s => s.IsDeleted)
            .HasColumnName("is_deleted");

        builder.Property(s => s.DeletedAt)
            .HasColumnName("deleted_at");

        // Indexes
        builder.HasIndex(s => new { s.CompanyId, s.Code })
            .IsUnique()
            .HasFilter("is_deleted = false");

        builder.HasIndex(s => new { s.CompanyId, s.Category, s.IsActive });
        builder.HasIndex(s => new { s.CompanyId, s.IsActive });
    }
}

