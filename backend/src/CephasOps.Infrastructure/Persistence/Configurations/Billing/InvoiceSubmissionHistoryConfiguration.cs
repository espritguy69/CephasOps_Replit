using CephasOps.Domain.Billing.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Billing;

public class InvoiceSubmissionHistoryConfiguration : IEntityTypeConfiguration<InvoiceSubmissionHistory>
{
    public void Configure(EntityTypeBuilder<InvoiceSubmissionHistory> builder)
    {
        builder.ToTable("InvoiceSubmissionHistory");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.InvoiceId)
            .IsRequired();

        builder.Property(x => x.SubmissionId)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.SubmittedAt)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.ResponseMessage)
            .HasMaxLength(1000);

        builder.Property(x => x.ResponseCode)
            .HasMaxLength(50);

        builder.Property(x => x.RejectionReason)
            .HasMaxLength(500);

        builder.Property(x => x.PortalType)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("MyInvois");

        builder.Property(x => x.SubmittedByUserId)
            .IsRequired();

        builder.Property(x => x.PaymentStatus)
            .HasMaxLength(50);

        builder.Property(x => x.PaymentReference)
            .HasMaxLength(200);

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        // Indexes
        builder.HasIndex(x => x.InvoiceId)
            .HasDatabaseName("IX_InvoiceSubmissionHistory_InvoiceId");

        builder.HasIndex(x => x.SubmissionId)
            .HasDatabaseName("IX_InvoiceSubmissionHistory_SubmissionId");

        builder.HasIndex(x => new { x.CompanyId, x.InvoiceId, x.IsActive })
            .HasDatabaseName("IX_InvoiceSubmissionHistory_CompanyId_InvoiceId_IsActive");

        builder.HasIndex(x => new { x.CompanyId, x.Status, x.SubmittedAt })
            .HasDatabaseName("IX_InvoiceSubmissionHistory_CompanyId_Status_SubmittedAt");
    }
}

