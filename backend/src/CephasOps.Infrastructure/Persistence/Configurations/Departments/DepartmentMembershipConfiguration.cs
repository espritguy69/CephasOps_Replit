using CephasOps.Domain.Departments.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CephasOps.Infrastructure.Persistence.Configurations.Departments;

public class DepartmentMembershipConfiguration : IEntityTypeConfiguration<DepartmentMembership>
{
    public void Configure(EntityTypeBuilder<DepartmentMembership> builder)
    {
        builder.ToTable("DepartmentMemberships");

        builder.HasKey(dm => dm.Id);

        builder.HasIndex(dm => new { dm.UserId, dm.DepartmentId })
            .IsUnique();

        builder.Property(dm => dm.Role)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasOne(dm => dm.Department)
            .WithMany(d => d.Memberships)
            .HasForeignKey(dm => dm.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(dm => dm.User)
            .WithMany(u => u.DepartmentMemberships)
            .HasForeignKey(dm => dm.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}


