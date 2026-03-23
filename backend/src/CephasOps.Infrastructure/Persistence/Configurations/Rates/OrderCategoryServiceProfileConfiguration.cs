using CephasOps.Domain.Rates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Rates;

public class OrderCategoryServiceProfileConfiguration : IEntityTypeConfiguration<OrderCategoryServiceProfile>
{
    public void Configure(EntityTypeBuilder<OrderCategoryServiceProfile> builder)
    {
        builder.ToTable("OrderCategoryServiceProfiles");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.OrderCategory)
            .WithMany()
            .HasForeignKey(x => x.OrderCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ServiceProfile)
            .WithMany()
            .HasForeignKey(x => x.ServiceProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        // One active mapping per order category per company
        builder.HasIndex(x => new { x.CompanyId, x.OrderCategoryId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
    }
}
