using CephasOps.Domain.Settings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Settings;

public class GuardConditionDefinitionConfiguration : IEntityTypeConfiguration<GuardConditionDefinition>
{
    public void Configure(EntityTypeBuilder<GuardConditionDefinition> builder)
    {
        builder.ToTable("guard_condition_definitions");

        builder.HasKey(gcd => gcd.Id);

        builder.Property(gcd => gcd.CompanyId)
            .HasColumnName("company_id");

        builder.Property(gcd => gcd.Key)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("key");

        builder.Property(gcd => gcd.Name)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("name");

        builder.Property(gcd => gcd.Description)
            .HasMaxLength(1000)
            .HasColumnName("description");

        builder.Property(gcd => gcd.EntityType)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("entity_type");

        builder.Property(gcd => gcd.ValidatorType)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("validator_type");

        builder.Property(gcd => gcd.ValidatorConfigJson)
            .HasColumnType("jsonb")
            .HasColumnName("validator_config_json");

        builder.Property(gcd => gcd.IsActive)
            .HasColumnName("is_active");

        builder.Property(gcd => gcd.DisplayOrder)
            .HasColumnName("display_order");

        builder.Property(gcd => gcd.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(gcd => gcd.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(gcd => gcd.IsDeleted)
            .HasColumnName("is_deleted");

        builder.Property(gcd => gcd.DeletedAt)
            .HasColumnName("deleted_at");

        builder.Property(gcd => gcd.RowVersion)
            .HasColumnName("row_version")
            .IsRowVersion();

        // Unique constraint: CompanyId + EntityType + Key
        builder.HasIndex(gcd => new { gcd.CompanyId, gcd.EntityType, gcd.Key })
            .IsUnique()
            .HasFilter("is_deleted = false");

        // Index for filtering by entity type and active status
        builder.HasIndex(gcd => new { gcd.CompanyId, gcd.EntityType, gcd.IsActive });
    }
}

