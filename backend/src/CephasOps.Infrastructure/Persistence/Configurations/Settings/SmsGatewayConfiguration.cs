using CephasOps.Domain.Settings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Settings;

public class SmsGatewayConfiguration : IEntityTypeConfiguration<SmsGateway>
{
    public void Configure(EntityTypeBuilder<SmsGateway> builder)
    {
        builder.ToTable("sms_gateways");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.DeviceName)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("device_name");

        builder.Property(s => s.BaseUrl)
            .IsRequired()
            .HasMaxLength(500)
            .HasColumnName("base_url");

        builder.Property(s => s.ApiKey)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("api_key");

        builder.Property(s => s.LastSeenAtUtc)
            .IsRequired()
            .HasColumnName("last_seen_at_utc");

        builder.Property(s => s.IsActive)
            .IsRequired()
            .HasColumnName("is_active");

        builder.Property(s => s.AdditionalInfo)
            .HasMaxLength(1000)
            .HasColumnName("additional_info");

        builder.Property(s => s.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at");

        builder.Property(s => s.UpdatedAt)
            .IsRequired()
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(s => s.IsActive)
            .HasFilter("is_active = true");

        builder.HasIndex(s => s.LastSeenAtUtc);
    }
}

