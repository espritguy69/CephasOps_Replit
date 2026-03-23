using CephasOps.Domain.Settings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Settings;

public class CustomerPreferenceConfiguration : IEntityTypeConfiguration<CustomerPreference>
{
    public void Configure(EntityTypeBuilder<CustomerPreference> builder)
    {
        builder.ToTable("customer_preferences");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.CustomerPhone)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("customer_phone");

        builder.Property(c => c.UsesWhatsApp)
            .HasColumnName("uses_whatsapp");

        builder.Property(c => c.LastWhatsAppCheck)
            .HasColumnName("last_whatsapp_check");

        builder.Property(c => c.LastWhatsAppSuccess)
            .HasColumnName("last_whatsapp_success");

        builder.Property(c => c.LastWhatsAppFailure)
            .HasColumnName("last_whatsapp_failure");

        builder.Property(c => c.ConsecutiveWhatsAppFailures)
            .IsRequired()
            .HasColumnName("consecutive_whatsapp_failures");

        builder.Property(c => c.PreferredChannel)
            .HasMaxLength(50)
            .HasColumnName("preferred_channel");

        builder.Property(c => c.Notes)
            .HasMaxLength(1000)
            .HasColumnName("notes");

        builder.Property(c => c.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at");

        builder.Property(c => c.UpdatedAt)
            .IsRequired()
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(c => c.CustomerPhone)
            .IsUnique();

        builder.HasIndex(c => c.UsesWhatsApp);
        builder.HasIndex(c => c.LastWhatsAppCheck);
    }
}

