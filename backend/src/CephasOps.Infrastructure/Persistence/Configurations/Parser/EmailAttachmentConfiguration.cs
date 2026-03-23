using CephasOps.Domain.Parser.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Parser;

public class EmailAttachmentConfiguration : IEntityTypeConfiguration<EmailAttachment>
{
    public void Configure(EntityTypeBuilder<EmailAttachment> builder)
    {
        builder.ToTable("EmailAttachments");

        builder.HasKey(ea => ea.Id);

        builder.Property(ea => ea.FileName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(ea => ea.ContentType)
            .IsRequired()
            .HasMaxLength(255)
            .HasDefaultValue("application/octet-stream");

        builder.Property(ea => ea.SizeBytes)
            .IsRequired();

        builder.Property(ea => ea.StoragePath)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(ea => ea.ContentId)
            .HasMaxLength(255);

        builder.Property(ea => ea.ExpiresAt)
            .IsRequired();

        // Index for cleanup job (expired attachments)
        builder.HasIndex(ea => new { ea.ExpiresAt, ea.CompanyId });

        // Index for email lookup
        builder.HasIndex(ea => new { ea.EmailMessageId, ea.CompanyId });

        // Foreign key to EmailMessage
        builder.HasOne(ea => ea.EmailMessage)
            .WithMany(em => em.Attachments)
            .HasForeignKey(ea => ea.EmailMessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

