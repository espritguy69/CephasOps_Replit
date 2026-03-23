using CephasOps.Domain.Rates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Rates;

public class PayoutAnomalyAlertRunConfiguration : IEntityTypeConfiguration<PayoutAnomalyAlertRun>
{
    public void Configure(EntityTypeBuilder<PayoutAnomalyAlertRun> builder)
    {
        builder.ToTable("PayoutAnomalyAlertRuns");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TriggerSource).HasMaxLength(32);
        builder.HasIndex(x => x.StartedAt).IsDescending();
    }
}
