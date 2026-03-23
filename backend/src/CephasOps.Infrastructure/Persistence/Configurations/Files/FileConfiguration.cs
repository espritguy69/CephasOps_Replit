using FileEntity = CephasOps.Domain.Files.Entities.File;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Files;

public class FileConfiguration : IEntityTypeConfiguration<FileEntity>
{
    public void Configure(EntityTypeBuilder<FileEntity> builder)
    {
        builder.ToTable("Files");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.FileName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(f => f.StoragePath)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(f => f.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(f => f.SizeBytes)
            .IsRequired();

        builder.Property(f => f.Checksum)
            .HasMaxLength(100);

        builder.Property(f => f.Module)
            .HasMaxLength(50);

        builder.Property(f => f.EntityType)
            .HasMaxLength(50);

        builder.Property(f => f.StorageTier)
            .HasMaxLength(20)
            .HasDefaultValue("Hot");

        builder.HasIndex(f => new { f.CompanyId, f.StorageTier });

        builder.HasIndex(f => new { f.CompanyId, f.Id });
        builder.HasIndex(f => new { f.CompanyId, f.EntityId, f.EntityType });
        builder.HasIndex(f => new { f.CompanyId, f.CreatedAt }).HasDatabaseName("IX_Files_CompanyId_CreatedAt");
        builder.HasIndex(f => f.CreatedAt);
    }
}

