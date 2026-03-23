using CephasOps.Domain.Parser.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Parser;

public class ParserTemplateConfiguration : IEntityTypeConfiguration<ParserTemplate>
{
    public void Configure(EntityTypeBuilder<ParserTemplate> builder)
    {
        builder.ToTable("ParserTemplates");

        builder.HasKey(pt => pt.Id);

        builder.Property(pt => pt.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(pt => pt.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(pt => pt.PartnerPattern)
            .HasMaxLength(500);

        builder.Property(pt => pt.SubjectPattern)
            .HasMaxLength(500);

        builder.Property(pt => pt.OrderTypeCode)
            .HasMaxLength(50);

        builder.Property(pt => pt.Description)
            .HasMaxLength(1000);

        builder.Property(pt => pt.ExpectedAttachmentTypes)
            .HasMaxLength(200);

        builder.HasIndex(pt => new { pt.CompanyId, pt.Code })
            .IsUnique();

        builder.HasIndex(pt => new { pt.CompanyId, pt.Priority, pt.IsActive });
    }
}

