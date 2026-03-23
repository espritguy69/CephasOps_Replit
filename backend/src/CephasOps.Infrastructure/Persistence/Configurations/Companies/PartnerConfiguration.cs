using CephasOps.Domain.Companies.Entities;
using CephasOps.Domain.Departments.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Companies;

public class PartnerConfiguration : IEntityTypeConfiguration<Partner>
{
    public void Configure(EntityTypeBuilder<Partner> builder)
    {
        builder.ToTable("Partners");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(p => p.Code)
            .HasMaxLength(50);

        builder.Property(p => p.PartnerType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.ContactName)
            .HasMaxLength(200);

        builder.Property(p => p.ContactEmail)
            .HasMaxLength(255);

        builder.Property(p => p.ContactPhone)
            .HasMaxLength(50);

        // Optional department assignment
        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(p => p.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(p => new { p.CompanyId, p.Name });
        builder.HasIndex(p => new { p.CompanyId, p.IsActive });
        builder.HasIndex(p => new { p.CompanyId, p.DepartmentId });
    }
}

