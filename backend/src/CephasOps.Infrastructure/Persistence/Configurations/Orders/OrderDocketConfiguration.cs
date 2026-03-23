using CephasOps.Domain.Orders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Orders;

public class OrderDocketConfiguration : IEntityTypeConfiguration<OrderDocket>
{
    public void Configure(EntityTypeBuilder<OrderDocket> builder)
    {
        builder.ToTable("OrderDockets");

        builder.HasKey(od => od.Id);

        builder.Property(od => od.UploadSource)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(od => od.Notes)
            .HasMaxLength(2000);

        builder.HasIndex(od => new { od.CompanyId, od.OrderId });
        builder.HasIndex(od => new { od.CompanyId, od.OrderId, od.IsFinal });
    }
}

