using CephasOps.Domain.Parser.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Parser;

public class EmailTemplateConfiguration : IEntityTypeConfiguration<EmailTemplate>
{
    public void Configure(EntityTypeBuilder<EmailTemplate> builder)
    {
        builder.ToTable("EmailTemplates");

        builder.HasKey(et => et.Id);

        builder.Property(et => et.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(et => et.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(et => et.SubjectTemplate)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(et => et.BodyTemplate)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(et => et.RelatedEntityType)
            .HasMaxLength(50);

        builder.Property(et => et.ReplyPattern)
            .HasMaxLength(200);

        builder.Property(et => et.Description)
            .HasMaxLength(1000);

        builder.Property(et => et.Direction)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("Outgoing");

        builder.HasIndex(et => new { et.CompanyId, et.Code })
            .IsUnique();

        builder.HasIndex(et => new { et.CompanyId, et.Direction, et.IsActive });

        builder.HasIndex(et => new { et.CompanyId, et.Priority, et.IsActive });

        builder.HasIndex(et => new { et.CompanyId, et.DepartmentId, et.IsActive });
    }
}

