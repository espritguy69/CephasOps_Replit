using CephasOps.Domain.Parser.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Parser;

public class ParseSessionConfiguration : IEntityTypeConfiguration<ParseSession>
{
    public void Configure(EntityTypeBuilder<ParseSession> builder)
    {
        builder.ToTable("ParseSessions");

        builder.HasKey(ps => ps.Id);

        builder.Property(ps => ps.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ps => ps.ErrorMessage)
            .HasMaxLength(2000);

        // Configure RowVersion for optimistic concurrency
        // PostgreSQL uses bytea with xmin or a trigger, but for simplicity we'll use a computed value
        builder.Property(ps => ps.RowVersion)
            .IsConcurrencyToken()
            .ValueGeneratedOnAddOrUpdate()
            .HasDefaultValueSql("gen_random_bytes(8)");

        // Ensure EmailMessageId is nullable (for file uploads)
        builder.Property(ps => ps.EmailMessageId)
            .IsRequired(false);

        // Configure DateTime properties to ensure UTC for PostgreSQL
        // PostgreSQL requires explicit UTC DateTime values
        // Note: Using if-else instead of switch expressions because EF Core expression trees don't support switch expressions
        builder.Property(ps => ps.CreatedAt)
            .HasConversion(
                v => v.Kind == DateTimeKind.Utc ? v : (v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : DateTime.SpecifyKind(v, DateTimeKind.Utc)),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        builder.Property(ps => ps.UpdatedAt)
            .HasConversion(
                v => v.Kind == DateTimeKind.Utc ? v : (v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : DateTime.SpecifyKind(v, DateTimeKind.Utc)),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        builder.Property(ps => ps.CompletedAt)
            .HasConversion(
                v => v.HasValue
                    ? (v.Value.Kind == DateTimeKind.Utc ? v.Value : (v.Value.Kind == DateTimeKind.Local ? v.Value.ToUniversalTime() : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)))
                    : (DateTime?)null,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : null);

        builder.HasIndex(ps => new { ps.CompanyId, ps.EmailMessageId });
        builder.HasIndex(ps => new { ps.CompanyId, ps.Status, ps.CreatedAt });
    }
}

