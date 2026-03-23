using CephasOps.Domain.Parser.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Parser;

public class ParserReplayRunConfiguration : IEntityTypeConfiguration<ParserReplayRun>
{
    public void Configure(EntityTypeBuilder<ParserReplayRun> builder)
    {
        builder.ToTable("ParserReplayRuns");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.TriggeredBy)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.OldParseStatus)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.NewParseStatus)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.OldMissingFields)
            .HasColumnType("jsonb");

        builder.Property(r => r.NewMissingFields)
            .HasColumnType("jsonb");

        builder.Property(r => r.ResultSummary)
            .HasColumnType("jsonb");

        builder.Property(r => r.OldSheetName).HasMaxLength(255);
        builder.Property(r => r.NewSheetName).HasMaxLength(255);

        builder.Property(r => r.CreatedAt)
            .HasConversion(
                v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        builder.HasIndex(r => r.CreatedAt);
        builder.HasIndex(r => r.AttachmentId);
        builder.HasIndex(r => r.OriginalParseSessionId);
        builder.HasIndex(r => new { r.RegressionDetected, r.CreatedAt });
    }
}
