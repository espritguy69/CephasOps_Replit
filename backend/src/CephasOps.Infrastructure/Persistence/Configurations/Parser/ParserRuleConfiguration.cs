using CephasOps.Domain.Parser.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Parser;

public class ParserRuleConfiguration : IEntityTypeConfiguration<ParserRule>
{
    public void Configure(EntityTypeBuilder<ParserRule> builder)
    {
        builder.ToTable("ParserRules");

        builder.HasKey(pr => pr.Id);

        builder.Property(pr => pr.FromAddressPattern)
            .HasMaxLength(500);

        builder.Property(pr => pr.DomainPattern)
            .HasMaxLength(200);

        builder.Property(pr => pr.SubjectContains)
            .HasMaxLength(500);

        builder.Property(pr => pr.ActionType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(pr => pr.Description)
            .HasMaxLength(1000);

        builder.HasIndex(pr => new { pr.CompanyId, pr.Priority, pr.IsActive });
        builder.HasIndex(pr => new { pr.CompanyId, pr.EmailAccountId, pr.IsActive });
    }
}

