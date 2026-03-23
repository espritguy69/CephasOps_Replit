using CephasOps.Domain.Pnl.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Pnl;

public class PnlTypeConfiguration : IEntityTypeConfiguration<PnlType>
{
    public void Configure(EntityTypeBuilder<PnlType> builder)
    {
        builder.ToTable("PnlTypes");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(t => t.Description)
            .HasMaxLength(500);

        builder.Property(t => t.Category)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(t => t.SortOrder)
            .HasDefaultValue(0);

        builder.Property(t => t.IsActive)
            .HasDefaultValue(true);

        builder.Property(t => t.IsTransactional)
            .HasDefaultValue(true);

        // Self-referencing relationship for hierarchy
        builder.HasOne(t => t.Parent)
            .WithMany(t => t.Children)
            .HasForeignKey(t => t.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => new { t.CompanyId, t.Code }).IsUnique();
        builder.HasIndex(t => new { t.CompanyId, t.Category });
        builder.HasIndex(t => new { t.CompanyId, t.ParentId });
        builder.HasIndex(t => t.IsActive);
    }
}

