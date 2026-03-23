using CephasOps.Domain.Settings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Settings;

public class WhatsAppTemplateConfiguration : IEntityTypeConfiguration<WhatsAppTemplate>
{
    public void Configure(EntityTypeBuilder<WhatsAppTemplate> builder)
    {
        builder.ToTable("whatsapp_templates");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Code)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("code");

        builder.Property(w => w.Name)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("name");

        builder.Property(w => w.Description)
            .HasMaxLength(500)
            .HasColumnName("description");

        builder.Property(w => w.Category)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("category");

        builder.Property(w => w.TemplateId)
            .HasMaxLength(200)
            .HasColumnName("template_id");

        builder.Property(w => w.ApprovalStatus)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("approval_status");

        builder.Property(w => w.MessageBody)
            .HasMaxLength(2000)
            .HasColumnName("message_body");

        builder.Property(w => w.Language)
            .HasMaxLength(10)
            .HasColumnName("language");

        builder.Property(w => w.IsActive)
            .HasColumnName("is_active");

        builder.Property(w => w.CreatedByUserId)
            .HasColumnName("created_by_user_id");

        builder.Property(w => w.UpdatedByUserId)
            .HasColumnName("updated_by_user_id");

        builder.Property(w => w.Notes)
            .HasMaxLength(1000)
            .HasColumnName("notes");

        builder.Property(w => w.SubmittedAt)
            .HasColumnName("submitted_at");

        builder.Property(w => w.ApprovedAt)
            .HasColumnName("approved_at");

        builder.Property(w => w.CompanyId)
            .HasColumnName("company_id");

        builder.Property(w => w.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(w => w.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(w => w.IsDeleted)
            .HasColumnName("is_deleted");

        builder.Property(w => w.DeletedAt)
            .HasColumnName("deleted_at");

        // Indexes
        builder.HasIndex(w => new { w.CompanyId, w.Code })
            .IsUnique()
            .HasFilter("is_deleted = false");

        builder.HasIndex(w => new { w.CompanyId, w.Category, w.ApprovalStatus, w.IsActive });
        builder.HasIndex(w => new { w.CompanyId, w.ApprovalStatus, w.IsActive });
        builder.HasIndex(w => w.TemplateId);
    }
}

