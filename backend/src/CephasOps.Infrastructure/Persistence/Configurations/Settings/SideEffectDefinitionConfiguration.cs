using CephasOps.Domain.Settings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Settings;

public class SideEffectDefinitionConfiguration : IEntityTypeConfiguration<SideEffectDefinition>
{
    public void Configure(EntityTypeBuilder<SideEffectDefinition> builder)
    {
        builder.ToTable("side_effect_definitions");

        builder.HasKey(sed => sed.Id);

        builder.Property(sed => sed.CompanyId)
            .HasColumnName("company_id");

        builder.Property(sed => sed.Key)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("key");

        builder.Property(sed => sed.Name)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("name");

        builder.Property(sed => sed.Description)
            .HasMaxLength(1000)
            .HasColumnName("description");

        builder.Property(sed => sed.EntityType)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("entity_type");

        builder.Property(sed => sed.ExecutorType)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("executor_type");

        builder.Property(sed => sed.ExecutorConfigJson)
            .HasColumnType("jsonb")
            .HasColumnName("executor_config_json");

        builder.Property(sed => sed.IsActive)
            .HasColumnName("is_active");

        builder.Property(sed => sed.DisplayOrder)
            .HasColumnName("display_order");

        builder.Property(sed => sed.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(sed => sed.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(sed => sed.IsDeleted)
            .HasColumnName("is_deleted");

        builder.Property(sed => sed.DeletedAt)
            .HasColumnName("deleted_at");

        builder.Property(sed => sed.RowVersion)
            .HasColumnName("row_version")
            .IsRowVersion();

        // Unique constraint: CompanyId + EntityType + Key
        builder.HasIndex(sed => new { sed.CompanyId, sed.EntityType, sed.Key })
            .IsUnique()
            .HasFilter("is_deleted = false");

        // Index for filtering by entity type and active status
        builder.HasIndex(sed => new { sed.CompanyId, sed.EntityType, sed.IsActive });
    }
}

