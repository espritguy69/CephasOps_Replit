using CephasOps.Domain.Integration.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Integration;

public class OutboundIntegrationAttemptConfiguration : IEntityTypeConfiguration<OutboundIntegrationAttempt>
{
    public void Configure(EntityTypeBuilder<OutboundIntegrationAttempt> builder)
    {
        builder.ToTable("OutboundIntegrationAttempts");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ResponseBodySnippet).HasMaxLength(2000);
        builder.Property(e => e.ErrorMessage).HasMaxLength(2000);

        builder.HasOne(e => e.OutboundDelivery)
            .WithMany()
            .HasForeignKey(e => e.OutboundDeliveryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.OutboundDeliveryId);
    }
}
