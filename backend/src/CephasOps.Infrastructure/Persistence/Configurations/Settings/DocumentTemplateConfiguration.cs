using CephasOps.Domain.Settings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Settings;

public class DocumentTemplateConfiguration : IEntityTypeConfiguration<DocumentTemplate>
{
    public void Configure(EntityTypeBuilder<DocumentTemplate> builder)
    {
        builder.ToTable("DocumentTemplates");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(d => d.DocumentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.Engine)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(d => d.HtmlBody)
            .IsRequired();

        builder.Property(d => d.Description)
            .HasMaxLength(1000);

        builder.Property(d => d.Tags)
            .HasMaxLength(1000);

        // TemplateFileId is nullable - used for CarboneDocx engine
        builder.Property(d => d.TemplateFileId);

        builder.HasIndex(d => new { d.CompanyId, d.DocumentType, d.PartnerId, d.IsActive });
        builder.HasIndex(d => new { d.CompanyId, d.IsActive });
    }
}

public class GeneratedDocumentConfiguration : IEntityTypeConfiguration<GeneratedDocument>
{
    public void Configure(EntityTypeBuilder<GeneratedDocument> builder)
    {
        builder.ToTable("GeneratedDocuments");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.DocumentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(g => g.ReferenceEntity)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(g => g.Format)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(g => g.MetadataJson)
            .HasMaxLength(2000);

        builder.HasIndex(g => new { g.CompanyId, g.ReferenceEntity, g.ReferenceId });
        builder.HasIndex(g => new { g.CompanyId, g.DocumentType, g.GeneratedAt });
        builder.HasIndex(g => g.TemplateId);
        builder.HasIndex(g => g.FileId);
    }
}

public class DocumentPlaceholderDefinitionConfiguration : IEntityTypeConfiguration<DocumentPlaceholderDefinition>
{
    public void Configure(EntityTypeBuilder<DocumentPlaceholderDefinition> builder)
    {
        builder.ToTable("DocumentPlaceholderDefinitions");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.DocumentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.Key)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(d => d.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(d => d.ExampleValue)
            .HasMaxLength(500);

        builder.HasIndex(d => d.DocumentType);
        builder.HasIndex(d => new { d.DocumentType, d.Key })
            .IsUnique();
    }
}

