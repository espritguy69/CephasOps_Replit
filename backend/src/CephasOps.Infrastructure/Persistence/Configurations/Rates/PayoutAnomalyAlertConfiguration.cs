using CephasOps.Domain.Rates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Rates;

public class PayoutAnomalyAlertConfiguration : IEntityTypeConfiguration<PayoutAnomalyAlert>
{
    public void Configure(EntityTypeBuilder<PayoutAnomalyAlert> builder)
    {
        builder.ToTable("PayoutAnomalyAlerts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AnomalyFingerprintId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Channel).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);
        builder.Property(x => x.RecipientId).HasMaxLength(256);

        builder.HasIndex(x => x.AnomalyFingerprintId);
        builder.HasIndex(x => x.SentAtUtc);
        builder.HasIndex(x => new { x.AnomalyFingerprintId, x.Channel });
    }
}
