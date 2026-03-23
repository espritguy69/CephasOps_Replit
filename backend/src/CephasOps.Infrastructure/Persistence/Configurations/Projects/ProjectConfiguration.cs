using CephasOps.Domain.Projects.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Projects;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ProjectCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(2000);

        builder.Property(e => e.ProjectType)
            .HasMaxLength(50);

        builder.Property(e => e.Status)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.CustomerName)
            .HasMaxLength(200);

        builder.Property(e => e.CustomerPhone)
            .HasMaxLength(50);

        builder.Property(e => e.CustomerEmail)
            .HasMaxLength(200);

        builder.Property(e => e.SiteAddress)
            .HasMaxLength(500);

        builder.Property(e => e.City)
            .HasMaxLength(100);

        builder.Property(e => e.State)
            .HasMaxLength(100);

        builder.Property(e => e.Postcode)
            .HasMaxLength(20);

        builder.Property(e => e.GpsCoordinates)
            .HasMaxLength(100);

        builder.Property(e => e.BudgetAmount)
            .HasPrecision(18, 2);

        builder.Property(e => e.ContractValue)
            .HasPrecision(18, 2);

        builder.Property(e => e.Currency)
            .HasMaxLength(10);

        builder.Property(e => e.Notes)
            .HasMaxLength(4000);

        builder.HasIndex(e => new { e.CompanyId, e.ProjectCode })
            .IsUnique();

        builder.HasIndex(e => new { e.CompanyId, e.Status });
        builder.HasIndex(e => new { e.CompanyId, e.ProjectType });
        builder.HasIndex(e => new { e.CompanyId, e.PartnerId });

        builder.HasMany(e => e.BoqItems)
            .WithOne(e => e.Project)
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class BoqItemConfiguration : IEntityTypeConfiguration<BoqItem>
{
    public void Configure(EntityTypeBuilder<BoqItem> builder)
    {
        builder.ToTable("boq_items");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Section)
            .HasMaxLength(100);

        builder.Property(e => e.ItemType)
            .HasMaxLength(50);

        builder.Property(e => e.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.Sku)
            .HasMaxLength(100);

        builder.Property(e => e.Unit)
            .HasMaxLength(20);

        builder.Property(e => e.Quantity)
            .HasPrecision(18, 4);

        builder.Property(e => e.UnitRate)
            .HasPrecision(18, 4);

        builder.Property(e => e.Total)
            .HasPrecision(18, 2);

        builder.Property(e => e.MarkupPercent)
            .HasPrecision(5, 2);

        builder.Property(e => e.SellingPrice)
            .HasPrecision(18, 2);

        builder.Property(e => e.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(e => new { e.ProjectId, e.LineNumber });
        builder.HasIndex(e => new { e.ProjectId, e.Section });
    }
}

