using CephasOps.Domain.ServiceInstallers.Entities;
using CephasOps.Domain.ServiceInstallers.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.ServiceInstallers;

public class ServiceInstallerConfiguration : IEntityTypeConfiguration<ServiceInstaller>
{
    public void Configure(EntityTypeBuilder<ServiceInstaller> builder)
    {
        builder.ToTable("ServiceInstallers", table => 
        {
            // Add CHECK constraint for SiLevel (only Senior or Junior allowed)
            table.HasCheckConstraint("CK_ServiceInstallers_SiLevel", 
                "\"SiLevel\" IN ('Junior', 'Senior')");
        });

        builder.HasKey(si => si.Id);

        builder.Property(si => si.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(si => si.EmployeeId)
            .HasMaxLength(50);

        builder.Property(si => si.Phone)
            .HasMaxLength(50);

        builder.Property(si => si.Email)
            .HasMaxLength(255);

        builder.Property(si => si.SiLevel)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired()
            .HasDefaultValue(InstallerLevel.Junior);

        builder.Property(si => si.InstallerType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired()
            .HasDefaultValue(InstallerType.InHouse);
        
        builder.Property(si => si.AvailabilityStatus)
            .HasMaxLength(50);
        
        builder.Property(si => si.EmploymentStatus)
            .HasMaxLength(50);
        
        builder.Property(si => si.ContractorId)
            .HasMaxLength(100);
        
        builder.Property(si => si.ContractorCompany)
            .HasMaxLength(200);

        builder.Property(si => si.IcNumber)
            .HasMaxLength(50);

        builder.Property(si => si.BankName)
            .HasMaxLength(200);

        builder.Property(si => si.BankAccountNumber)
            .HasMaxLength(50);

        builder.Property(si => si.Address)
            .HasMaxLength(500);

        builder.Property(si => si.EmergencyContact)
            .HasMaxLength(200);

        builder.HasIndex(si => new { si.CompanyId, si.EmployeeId });
        builder.HasIndex(si => new { si.CompanyId, si.IsActive });
        builder.HasIndex(si => si.UserId);
    }
}

