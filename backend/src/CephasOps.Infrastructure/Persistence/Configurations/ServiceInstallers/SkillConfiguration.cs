using CephasOps.Domain.ServiceInstallers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.ServiceInstallers;

public class SkillConfiguration : IEntityTypeConfiguration<Skill>
{
    public void Configure(EntityTypeBuilder<Skill> builder)
    {
        builder.ToTable("Skills");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Code)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.Category)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.Description)
            .HasMaxLength(1000);

        builder.HasIndex(s => new { s.CompanyId, s.DepartmentId, s.Code })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(s => new { s.CompanyId, s.DepartmentId, s.Category, s.IsActive });
        builder.HasIndex(s => s.DepartmentId);

        // Relationships
        builder.HasMany(s => s.InstallerSkills)
            .WithOne(sis => sis.Skill)
            .HasForeignKey(sis => sis.SkillId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

