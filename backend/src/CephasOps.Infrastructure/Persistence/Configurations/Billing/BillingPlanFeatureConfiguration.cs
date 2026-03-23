using CephasOps.Domain.Billing.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Billing;

public class BillingPlanFeatureConfiguration : IEntityTypeConfiguration<BillingPlanFeature>
{
    public void Configure(EntityTypeBuilder<BillingPlanFeature> builder)
    {
        builder.ToTable("BillingPlanFeatures");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FeatureKey).IsRequired().HasMaxLength(128);
        builder.HasIndex(x => new { x.BillingPlanId, x.FeatureKey }).IsUnique();
        builder.HasOne(x => x.BillingPlan)
            .WithMany()
            .HasForeignKey(x => x.BillingPlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
