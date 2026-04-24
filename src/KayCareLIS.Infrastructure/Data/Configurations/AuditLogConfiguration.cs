using KayCareLIS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KayCareLIS.Infrastructure.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.AuditLogId);
        builder.Property(a => a.AuditLogId).ValueGeneratedOnAdd();
        builder.Property(a => a.TenantId).IsRequired();
        builder.Property(a => a.UserId).IsRequired();
        builder.Property(a => a.UserEmail).HasMaxLength(256).IsRequired();
        builder.Property(a => a.Action).HasMaxLength(100).IsRequired();
        builder.Property(a => a.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(a => a.EntityId).IsRequired();
        builder.Property(a => a.Details).HasColumnType("nvarchar(max)");
        builder.Property(a => a.IpAddress).HasMaxLength(45);
        builder.Property(a => a.Timestamp).IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(a => new { a.TenantId, a.PatientId });
        builder.HasIndex(a => new { a.TenantId, a.UserId });
        builder.HasIndex(a => new { a.TenantId, a.Timestamp });
    }
}
