using CephasOps.Domain.ServiceInstallers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.ServiceInstallers;

public class ServiceInstallerSkillConfiguration : IEntityTypeConfiguration<ServiceInstallerSkill>
{
    public void Configure(EntityTypeBuilder<ServiceInstallerSkill> builder)
    {
        builder.ToTable("ServiceInstallerSkills");

        builder.HasKey(sis => sis.Id);

        builder.Property(sis => sis.Notes)
            .HasMaxLength(1000);

        // Unique constraint: one installer can only have one active assignment per skill
        builder.HasIndex(sis => new { sis.ServiceInstallerId, sis.SkillId, sis.IsActive })
            .IsUnique()
            .HasFilter("\"IsActive\" = true AND \"IsDeleted\" = false");

        // Indexes for querying
        builder.HasIndex(sis => sis.ServiceInstallerId);
        builder.HasIndex(sis => sis.SkillId);
        builder.HasIndex(sis => new { sis.ServiceInstallerId, sis.IsActive });

        // Relationships
        builder.HasOne(sis => sis.ServiceInstaller)
            .WithMany(si => si.Skills)
            .HasForeignKey(sis => sis.ServiceInstallerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sis => sis.Skill)
            .WithMany(s => s.InstallerSkills)
            .HasForeignKey(sis => sis.SkillId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

