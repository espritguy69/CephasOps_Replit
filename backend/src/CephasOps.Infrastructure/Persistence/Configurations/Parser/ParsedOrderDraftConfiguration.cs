using CephasOps.Domain.Parser.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Parser;

public class ParsedOrderDraftConfiguration : IEntityTypeConfiguration<ParsedOrderDraft>
{
    public void Configure(EntityTypeBuilder<ParsedOrderDraft> builder)
    {
        builder.ToTable("ParsedOrderDrafts");

        builder.HasKey(pod => pod.Id);

        builder.Property(pod => pod.ServiceId)
            .HasMaxLength(500);

        builder.Property(pod => pod.TicketId)
            .HasMaxLength(500);

        builder.Property(pod => pod.AwoNumber)
            .HasMaxLength(100);

        builder.Property(pod => pod.CustomerName)
            .HasMaxLength(500);

        builder.Property(pod => pod.CustomerPhone)
            .HasMaxLength(100);

        builder.Property(pod => pod.CustomerEmail)
            .HasMaxLength(500);

        builder.Property(pod => pod.AdditionalContactNumber)
            .HasMaxLength(100);

        builder.Property(pod => pod.Issue)
            .HasMaxLength(1000);

        builder.Property(pod => pod.AddressText)
            .HasMaxLength(2000);
        
        builder.Property(pod => pod.OldAddress)
            .HasMaxLength(2000);
        
        builder.Property(pod => pod.BuildingName)
            .HasMaxLength(500);
        
        builder.Property(pod => pod.BuildingStatus)
            .HasMaxLength(50);

        builder.Property(pod => pod.AppointmentWindow)
            .HasMaxLength(100);

        builder.Property(pod => pod.OrderTypeHint)
            .HasMaxLength(200);

        builder.Property(pod => pod.OrderTypeCode)
            .HasMaxLength(100);

        builder.Property(pod => pod.PackageName)
            .HasMaxLength(500);

        builder.Property(pod => pod.Bandwidth)
            .HasMaxLength(100);

        builder.Property(pod => pod.OnuSerialNumber)
            .HasMaxLength(200);

        builder.Property(pod => pod.OnuPassword)
            .HasMaxLength(200);

        builder.Property(pod => pod.Username)
            .HasMaxLength(200);

        builder.Property(pod => pod.Password)
            .HasMaxLength(200);

        builder.Property(pod => pod.InternetWanIp)
            .HasMaxLength(50);

        builder.Property(pod => pod.InternetLanIp)
            .HasMaxLength(50);

        builder.Property(pod => pod.InternetGateway)
            .HasMaxLength(50);

        builder.Property(pod => pod.InternetSubnetMask)
            .HasMaxLength(50);

        builder.Property(pod => pod.VoipServiceId)
            .HasMaxLength(200);

        builder.Property(pod => pod.Remarks)
            .HasMaxLength(4000);

        builder.Property(pod => pod.AdditionalInformation)
            .HasMaxLength(8000);

        builder.Property(pod => pod.SourceFileName)
            .HasMaxLength(1000);

        builder.Property(pod => pod.ValidationStatus)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(pod => pod.ValidationNotes)
            .HasMaxLength(4000);

        builder.Property(pod => pod.ConfidenceScore)
            .HasPrecision(5, 4);

        // Configure RowVersion for optimistic concurrency
        // PostgreSQL uses bytea with xmin or a trigger, but for simplicity we'll use a computed value
        builder.Property(pod => pod.RowVersion)
            .IsConcurrencyToken()
            .ValueGeneratedOnAddOrUpdate()
            .HasDefaultValueSql("gen_random_bytes(8)");

        // Configure DateTime properties to ensure UTC for PostgreSQL
        // Note: Using if-else instead of switch expressions because EF Core expression trees don't support switch expressions
        builder.Property(pod => pod.CreatedAt)
            .HasConversion(
                v => v.Kind == DateTimeKind.Utc ? v : (v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : DateTime.SpecifyKind(v, DateTimeKind.Utc)),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        builder.Property(pod => pod.UpdatedAt)
            .HasConversion(
                v => v.Kind == DateTimeKind.Utc ? v : (v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : DateTime.SpecifyKind(v, DateTimeKind.Utc)),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        builder.Property(pod => pod.AppointmentDate)
            .HasConversion(
                v => v.HasValue
                    ? (v.Value.Kind == DateTimeKind.Utc ? v.Value : (v.Value.Kind == DateTimeKind.Local ? v.Value.ToUniversalTime() : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)))
                    : (DateTime?)null,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : null);

        builder.HasIndex(pod => new { pod.CompanyId, pod.ParseSessionId });
        builder.HasIndex(pod => new { pod.CompanyId, pod.ValidationStatus });
        builder.HasIndex(pod => pod.CreatedOrderId);
    }
}

