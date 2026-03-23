using CephasOps.Domain.Rates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Rates;

public class PayoutAnomalyReviewConfiguration : IEntityTypeConfiguration<PayoutAnomalyReview>
{
    public void Configure(EntityTypeBuilder<PayoutAnomalyReview> builder)
    {
        builder.ToTable("PayoutAnomalyReviews");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AnomalyFingerprintId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.AnomalyType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Severity).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.NotesJson).HasColumnType("text");

        builder.HasIndex(x => x.AnomalyFingerprintId).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.DetectedAt);
    }
}
