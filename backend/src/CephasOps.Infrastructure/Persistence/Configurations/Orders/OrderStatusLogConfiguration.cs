using CephasOps.Domain.Orders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Orders;

public class OrderStatusLogConfiguration : IEntityTypeConfiguration<OrderStatusLog>
{
    public void Configure(EntityTypeBuilder<OrderStatusLog> builder)
    {
        builder.ToTable("OrderStatusLogs");

        builder.HasKey(osl => osl.Id);

        builder.Property(osl => osl.FromStatus)
            .HasMaxLength(50);

        builder.Property(osl => osl.ToStatus)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(osl => osl.TransitionReason)
            .HasMaxLength(500);

        builder.Property(osl => osl.Source)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(osl => osl.MetadataJson)
            .HasColumnType("jsonb");

        builder.HasIndex(osl => new { osl.CompanyId, osl.OrderId });
        builder.HasIndex(osl => new { osl.CompanyId, osl.CreatedAt });
        builder.HasIndex(osl => osl.OrderId);
    }
}

