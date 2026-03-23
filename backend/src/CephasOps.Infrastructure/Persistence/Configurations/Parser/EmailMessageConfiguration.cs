using CephasOps.Domain.Parser.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Parser;

public class EmailMessageConfiguration : IEntityTypeConfiguration<EmailMessage>
{
    public void Configure(EntityTypeBuilder<EmailMessage> builder)
    {
        builder.ToTable("EmailMessages");

        builder.HasKey(em => em.Id);

        builder.Property(em => em.MessageId)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(em => em.FromAddress)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(em => em.ToAddresses)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(em => em.CcAddresses)
            .HasMaxLength(1000);

        builder.Property(em => em.Subject)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(em => em.BodyPreview)
            .HasColumnType("text");

        builder.Property(em => em.BodyText)
            .HasColumnType("text");

        builder.Property(em => em.BodyHtml)
            .HasColumnType("text");

        builder.Property(em => em.ParserStatus)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(em => em.ParserError)
            .HasColumnType("text");

        builder.Property(em => em.Direction)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("Inbound");

        // Configure RowVersion for optimistic concurrency
        // In PostgreSQL, we need ValueGeneratedOnAddOrUpdate to generate values
        builder.Property(em => em.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken()
            .ValueGeneratedOnAddOrUpdate()
            .HasDefaultValueSql("gen_random_bytes(8)");

        builder.HasIndex(em => new { em.CompanyId, em.MessageId })
            .IsUnique();

        builder.HasIndex(em => new { em.CompanyId, em.ParserStatus, em.ReceivedAt });
        
        builder.HasIndex(em => new { em.CompanyId, em.Direction, em.ReceivedAt });
        
        // Index for cleanup job (expired emails)
        builder.HasIndex(em => new { em.ExpiresAt, em.CompanyId });
    }
}

