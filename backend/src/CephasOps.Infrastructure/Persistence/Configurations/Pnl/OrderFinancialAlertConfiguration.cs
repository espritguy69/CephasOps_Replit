using CephasOps.Domain.Pnl.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Pnl;

public class OrderFinancialAlertConfiguration : IEntityTypeConfiguration<OrderFinancialAlert>
{
    public void Configure(EntityTypeBuilder<OrderFinancialAlert> builder)
    {
        builder.ToTable("OrderFinancialAlerts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.AlertCode).IsRequired().HasMaxLength(64);
        builder.Property(a => a.Severity).IsRequired().HasMaxLength(32);
        builder.Property(a => a.Message).IsRequired().HasMaxLength(1024);

        builder.HasIndex(a => new { a.OrderId, a.AlertCode });
        builder.HasIndex(a => a.CompanyId);
        builder.HasIndex(a => a.CreatedAt);
        builder.HasIndex(a => a.IsActive);
    }
}
