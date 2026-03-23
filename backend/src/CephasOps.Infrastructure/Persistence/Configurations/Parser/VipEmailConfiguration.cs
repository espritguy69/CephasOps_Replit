using CephasOps.Domain.Parser.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Parser;

public class VipEmailConfiguration : IEntityTypeConfiguration<VipEmail>
{
    public void Configure(EntityTypeBuilder<VipEmail> builder)
    {
        builder.ToTable("VipEmails");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.EmailAddress)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(v => v.DisplayName)
            .HasMaxLength(200);

        builder.Property(v => v.Description)
            .HasMaxLength(1000);

        builder.Property(v => v.NotifyRole)
            .HasMaxLength(100);

        builder.HasIndex(v => new { v.CompanyId, v.EmailAddress })
            .IsUnique();

        builder.HasIndex(v => new { v.CompanyId, v.IsActive });

        builder.HasIndex(v => v.VipGroupId);
    }
}

