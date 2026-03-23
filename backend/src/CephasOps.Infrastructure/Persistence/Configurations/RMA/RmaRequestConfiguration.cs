using CephasOps.Domain.RMA.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.RMA;

public class RmaRequestConfiguration : IEntityTypeConfiguration<RmaRequest>
{
    public void Configure(EntityTypeBuilder<RmaRequest> builder)
    {
        builder.ToTable("RmaRequests");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.RmaNumber)
            .HasMaxLength(100);

        builder.Property(r => r.Reason)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(r => r.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasMany(r => r.Items)
            .WithOne(i => i.RmaRequest)
            .HasForeignKey(i => i.RmaRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.CompanyId, r.PartnerId });
        builder.HasIndex(r => new { r.CompanyId, r.Status });
        builder.HasIndex(r => new { r.CompanyId, r.RequestDate });
        builder.HasIndex(r => r.RmaNumber);
    }
}

