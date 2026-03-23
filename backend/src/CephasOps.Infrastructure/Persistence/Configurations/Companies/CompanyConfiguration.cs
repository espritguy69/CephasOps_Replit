using CephasOps.Domain.Companies.Entities;
using CephasOps.Domain.Companies.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Companies;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies");

        builder.HasKey(c => c.Id);

        builder.HasIndex(c => c.TenantId);

        builder.HasOne(c => c.Tenant)
            .WithMany()
            .HasForeignKey(c => c.TenantId)
            .IsRequired(false);

        builder.Property(c => c.LegalName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(c => c.ShortName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.RegistrationNo)
            .HasMaxLength(100);

        builder.Property(c => c.TaxId)
            .HasMaxLength(100);

        builder.Property(c => c.Vertical)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.Phone)
            .HasMaxLength(50);

        builder.Property(c => c.Email)
            .HasMaxLength(255);

        builder.Property(c => c.Code)
            .HasMaxLength(50);
        builder.Property(c => c.SubscriptionId);
        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.HasIndex(c => c.ShortName);
        builder.HasIndex(c => c.Status);
        builder.HasIndex(c => c.IsActive);
        builder.HasIndex(c => c.Code);
    }
}

