using CephasOps.Domain.Parser.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Parser;

public class EmailAccountConfiguration : IEntityTypeConfiguration<EmailAccount>
{
    public void Configure(EntityTypeBuilder<EmailAccount> builder)
    {
        builder.ToTable("EmailAccounts");

        builder.HasKey(ea => ea.Id);

        builder.Property(ea => ea.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(ea => ea.Provider)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ea => ea.Host)
            .HasMaxLength(255);

        builder.Property(ea => ea.Username)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(ea => ea.Password)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(ea => ea.SmtpHost)
            .HasMaxLength(255);

        builder.Property(ea => ea.SmtpUsername)
            .HasMaxLength(255);

        builder.Property(ea => ea.SmtpPassword)
            .HasMaxLength(512);

        builder.Property(ea => ea.SmtpFromAddress)
            .HasMaxLength(255);

        builder.Property(ea => ea.SmtpFromName)
            .HasMaxLength(255);

        builder.HasIndex(ea => new { ea.CompanyId, ea.Name });

        builder.HasOne(ea => ea.DefaultParserTemplate)
            .WithMany()
            .HasForeignKey(ea => ea.DefaultParserTemplateId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}


