using CephasOps.Domain.Billing.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Billing;

public class BillingPlanConfiguration : IEntityTypeConfiguration<BillingPlan>
{
    public void Configure(EntityTypeBuilder<BillingPlan> builder)
    {
        builder.ToTable("BillingPlans");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Name).IsRequired().HasMaxLength(256);
        builder.Property(b => b.Slug).IsRequired().HasMaxLength(64);
        builder.Property(b => b.Price).HasPrecision(18, 2);
        builder.Property(b => b.Currency).HasMaxLength(3);
        builder.HasIndex(b => b.Slug).IsUnique();
        builder.HasIndex(b => b.IsActive);
    }
}
