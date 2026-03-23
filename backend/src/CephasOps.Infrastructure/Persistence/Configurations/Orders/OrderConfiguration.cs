using CephasOps.Domain.Buildings.Entities;
using CephasOps.Domain.Companies.Entities;
using CephasOps.Domain.Orders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Orders;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);

        // Foreign key to OrderType
        builder.HasOne<OrderType>()
            .WithMany()
            .HasForeignKey(o => o.OrderTypeId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign key to Partner
        builder.HasOne(o => o.Partner)
            .WithMany()
            .HasForeignKey(o => o.PartnerId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign key to OrderCategory (nullable, only for Activation orders)
        builder.HasOne(o => o.OrderCategory)
            .WithMany()
            .HasForeignKey(o => o.OrderCategoryId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // Foreign key to InstallationMethod (nullable, for rate keying)
        builder.HasOne(o => o.InstallationMethod)
            .WithMany()
            .HasForeignKey(o => o.InstallationMethodId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(o => o.SourceSystem)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(o => o.ServiceIdType)
            .HasConversion<int>()
            .IsRequired(false);

        builder.Property(o => o.ServiceId)
            .IsRequired()
            .HasMaxLength(200);
        
        // Network Info fields
        builder.Property(o => o.NetworkPackage)
            .HasMaxLength(1000);
        
        builder.Property(o => o.NetworkBandwidth)
            .HasMaxLength(100);
        
        builder.Property(o => o.NetworkLoginId)
            .HasMaxLength(200);
        
        builder.Property(o => o.NetworkPassword)
            .HasMaxLength(200);
        
        builder.Property(o => o.OnuPasswordEncrypted)
            .HasMaxLength(500); // Encrypted data is base64, longer than plain text
        
        builder.Property(o => o.NetworkWanIp)
            .HasMaxLength(50);
        
        builder.Property(o => o.NetworkLanIp)
            .HasMaxLength(50);
        
        builder.Property(o => o.NetworkGateway)
            .HasMaxLength(50);
        
        builder.Property(o => o.NetworkSubnetMask)
            .HasMaxLength(50);
        
        // VOIP fields
        builder.Property(o => o.VoipPassword)
            .HasMaxLength(200);
        
        builder.Property(o => o.VoipIpAddressOnu)
            .HasMaxLength(50);
        
        builder.Property(o => o.VoipGatewayOnu)
            .HasMaxLength(50);
        
        builder.Property(o => o.VoipSubnetMaskOnu)
            .HasMaxLength(50);
        
        builder.Property(o => o.VoipIpAddressSrp)
            .HasMaxLength(50);
        
        builder.Property(o => o.VoipRemarks)
            .HasMaxLength(1000);

        builder.Property(o => o.TicketId)
            .HasMaxLength(200);

        builder.Property(o => o.AwoNumber)
            .HasMaxLength(200);

        builder.Property(o => o.ExternalRef)
            .HasMaxLength(200);

        builder.Property(o => o.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(o => o.StatusReason)
            .HasMaxLength(500);

        builder.Property(o => o.Priority)
            .HasMaxLength(20);

        builder.Property(o => o.BuildingName)
            .HasMaxLength(500);

        builder.Property(o => o.UnitNo)
            .HasMaxLength(50);

        builder.Property(o => o.AddressLine1)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(o => o.AddressLine2)
            .HasMaxLength(500);

        builder.Property(o => o.City)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(o => o.State)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(o => o.Postcode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(o => o.CustomerName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(o => o.CustomerPhone)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(o => o.CustomerPhone2)
            .HasMaxLength(50);

        builder.Property(o => o.CustomerEmail)
            .HasMaxLength(255);

        builder.Property(o => o.Issue)
            .HasMaxLength(1000);

        builder.Property(o => o.Solution)
            .HasMaxLength(2000);

        builder.Property(o => o.KpiCategory)
            .HasMaxLength(50);

        builder.Property(o => o.PnlPeriod)
            .HasMaxLength(10);

        // Docket number per ORDER_LIFECYCLE.md
        builder.Property(o => o.DocketNumber)
            .HasMaxLength(100);

        // Relocation fields per ORDERS_MODULE.md section 7
        builder.Property(o => o.RelocationType)
            .HasMaxLength(20); // "Indoor" or "Outdoor"

        builder.Property(o => o.OldLocationNote)
            .HasMaxLength(500);

        builder.Property(o => o.NewLocationNote)
            .HasMaxLength(500);

        // Splitter fields - required before Docket Verification
        builder.Property(o => o.SplitterNumber)
            .HasMaxLength(100);

        builder.Property(o => o.SplitterLocation)
            .HasMaxLength(200);

        builder.Property(o => o.SplitterPort)
            .HasMaxLength(50);

        // Indexes
        builder.HasIndex(o => new { o.CompanyId, o.ServiceId })
            .IsUnique();

        builder.HasIndex(o => new { o.CompanyId, o.CreatedAt }).HasDatabaseName("IX_Orders_CompanyId_CreatedAt");
        builder.HasIndex(o => new { o.CompanyId, o.Status, o.AppointmentDate });
        builder.HasIndex(o => new { o.CompanyId, o.AssignedSiId, o.AppointmentDate });
        builder.HasIndex(o => new { o.CompanyId, o.PartnerId });
        builder.HasIndex(o => new { o.CompanyId, o.BuildingId });
        builder.HasIndex(o => o.Status);
        
        // Index for rate lookups (OrderType + OrderCategory + InstallationMethod)
        builder.HasIndex(o => new { o.OrderTypeId, o.OrderCategoryId, o.InstallationMethodId });

        // Concurrency token for optimistic concurrency control
        builder.Property(o => o.RowVersion)
            .IsRowVersion();
    }
}

