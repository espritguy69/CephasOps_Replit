using CephasOps.Domain.Rates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Rates;

public class BaseWorkRateConfiguration : IEntityTypeConfiguration<BaseWorkRate>
{
    public void Configure(EntityTypeBuilder<BaseWorkRate> builder)
    {
        builder.ToTable("BaseWorkRates");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount).HasPrecision(18, 4);
        builder.Property(x => x.Currency).HasMaxLength(3);
        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.HasOne(x => x.RateGroup)
            .WithMany()
            .HasForeignKey(x => x.RateGroupId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.OrderCategory)
            .WithMany()
            .HasForeignKey(x => x.OrderCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ServiceProfile)
            .WithMany()
            .HasForeignKey(x => x.ServiceProfileId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.InstallationMethod)
            .WithMany()
            .HasForeignKey(x => x.InstallationMethodId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.OrderSubtype)
            .WithMany()
            .HasForeignKey(x => x.OrderSubtypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Lookup indexes for future resolution (Phase 3+)
        builder.HasIndex(x => new { x.CompanyId, x.RateGroupId, x.IsActive })
            .HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(x => new { x.RateGroupId, x.OrderCategoryId, x.InstallationMethodId, x.OrderSubtypeId })
            .HasDatabaseName("IX_BaseWorkRates_Lookup")
            .HasFilter("\"IsDeleted\" = false AND \"IsActive\" = true");
        builder.HasIndex(x => new { x.RateGroupId, x.ServiceProfileId, x.InstallationMethodId, x.OrderSubtypeId })
            .HasDatabaseName("IX_BaseWorkRates_ServiceProfileLookup")
            .HasFilter("\"IsDeleted\" = false AND \"IsActive\" = true");
    }
}
