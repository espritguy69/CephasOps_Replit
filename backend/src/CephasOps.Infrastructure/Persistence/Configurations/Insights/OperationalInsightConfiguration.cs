using CephasOps.Domain.Insights.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Insights;

public class OperationalInsightConfiguration : IEntityTypeConfiguration<OperationalInsight>
{
    public void Configure(EntityTypeBuilder<OperationalInsight> builder)
    {
        builder.ToTable("OperationalInsights");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).IsRequired().HasMaxLength(128);
        builder.Property(x => x.PayloadJson).HasMaxLength(8000);
        builder.Property(x => x.EntityType).HasMaxLength(64);
        builder.HasIndex(x => new { x.CompanyId, x.OccurredAtUtc });
        builder.HasIndex(x => new { x.CompanyId, x.Type });
    }
}
