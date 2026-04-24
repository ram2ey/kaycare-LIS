using KayCareLIS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KayCareLIS.Infrastructure.Data.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasKey(r => r.RoleId);
        builder.Property(r => r.RoleId).ValueGeneratedOnAdd();
        builder.Property(r => r.RoleName).HasMaxLength(50).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(200);
        builder.HasIndex(r => r.RoleName).IsUnique();

        builder.HasData(
            new Role { RoleId = 1, RoleName = "SuperAdmin",    Description = "Platform-level administrator" },
            new Role { RoleId = 2, RoleName = "Admin",         Description = "LIS administrator" },
            new Role { RoleId = 3, RoleName = "Doctor",        Description = "Ordering physician / pathologist" },
            new Role { RoleId = 4, RoleName = "LabTechnician", Description = "Laboratory technician / phlebotomist" },
            new Role { RoleId = 5, RoleName = "Receptionist",  Description = "Front desk / patient registration" },
            new Role { RoleId = 6, RoleName = "BillingOfficer", Description = "Billing and revenue cycle staff" }
        );
    }
}
