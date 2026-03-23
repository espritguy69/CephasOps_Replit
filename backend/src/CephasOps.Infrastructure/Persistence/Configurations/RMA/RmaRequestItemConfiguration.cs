using CephasOps.Domain.RMA.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.RMA;

public class RmaRequestItemConfiguration : IEntityTypeConfiguration<RmaRequestItem>
{
    public void Configure(EntityTypeBuilder<RmaRequestItem> builder)
    {
        builder.ToTable("RmaRequestItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Notes)
            .HasMaxLength(1000);

        builder.Property(i => i.Result)
            .HasMaxLength(50);

        builder.HasOne(i => i.RmaRequest)
            .WithMany(r => r.Items)
            .HasForeignKey(i => i.RmaRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => new { i.CompanyId, i.RmaRequestId });
        builder.HasIndex(i => i.SerialisedItemId);
    }
}

