using CephasOps.Domain.Rates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Rates;

public class PayoutSnapshotRepairRunConfiguration : IEntityTypeConfiguration<PayoutSnapshotRepairRun>
{
    public void Configure(EntityTypeBuilder<PayoutSnapshotRepairRun> builder)
    {
        builder.ToTable("PayoutSnapshotRepairRuns");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TriggerSource).HasMaxLength(32);
        builder.Property(x => x.ErrorOrderIdsJson).HasColumnType("text");
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.HasIndex(x => x.StartedAt).IsDescending();
    }
}
