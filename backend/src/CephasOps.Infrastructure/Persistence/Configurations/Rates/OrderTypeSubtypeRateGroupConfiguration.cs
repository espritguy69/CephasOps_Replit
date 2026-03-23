using CephasOps.Domain.Rates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Rates;

public class OrderTypeSubtypeRateGroupConfiguration : IEntityTypeConfiguration<OrderTypeSubtypeRateGroup>
{
    public void Configure(EntityTypeBuilder<OrderTypeSubtypeRateGroup> builder)
    {
        builder.ToTable("OrderTypeSubtypeRateGroups");
        builder.HasKey(x => x.Id);
        builder.HasOne(x => x.OrderType)
            .WithMany()
            .HasForeignKey(x => x.OrderTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.OrderSubtype)
            .WithMany()
            .HasForeignKey(x => x.OrderSubtypeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.RateGroup)
            .WithMany()
            .HasForeignKey(x => x.RateGroupId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.CompanyId, x.OrderTypeId, x.OrderSubtypeId })
            .IsUnique()
            .HasDatabaseName("IX_OrderTypeSubtypeRateGroups_Company_Type_Subtype");
        builder.HasIndex(x => x.RateGroupId);
    }
}
