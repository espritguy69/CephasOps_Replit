using CephasOps.Domain.Companies.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Companies;

public class CompanyDocumentConfiguration : IEntityTypeConfiguration<CompanyDocument>
{
    public void Configure(EntityTypeBuilder<CompanyDocument> builder)
    {
        builder.ToTable("CompanyDocuments");

        builder.HasKey(cd => cd.Id);

        builder.Property(cd => cd.Category)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(cd => cd.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(cd => cd.DocumentType)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(cd => cd.Notes)
            .HasMaxLength(2000);

        builder.Property(cd => cd.RelatedModule)
            .HasMaxLength(100);

        builder.HasIndex(cd => new { cd.CompanyId, cd.Category });
        builder.HasIndex(cd => new { cd.CompanyId, cd.ExpiryDate });
        builder.HasIndex(cd => new { cd.CompanyId, cd.IsCritical });
        builder.HasIndex(cd => cd.FileId);
    }
}

