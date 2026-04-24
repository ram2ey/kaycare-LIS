using KayCareLIS.Core.Entities;
using KayCareLIS.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KayCareLIS.Infrastructure.Data;

public class AppDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Tenant>           Tenants          => Set<Tenant>();
    public DbSet<FacilitySettings> FacilitySettings => Set<FacilitySettings>();
    public DbSet<Role>             Roles            => Set<Role>();
    public DbSet<User>             Users            => Set<User>();
    public DbSet<Patient>          Patients         => Set<Patient>();
    public DbSet<Appointment>      Appointments     => Set<Appointment>();
    public DbSet<LabTestCatalog>   LabTestCatalog   => Set<LabTestCatalog>();
    public DbSet<LabOrder>         LabOrders        => Set<LabOrder>();
    public DbSet<LabOrderItem>     LabOrderItems    => Set<LabOrderItem>();
    public DbSet<LabResult>        LabResults       => Set<LabResult>();
    public DbSet<LabObservation>   LabObservations  => Set<LabObservation>();
    public DbSet<IcdCode>          IcdCodes         => Set<IcdCode>();
    public DbSet<Bill>             Bills            => Set<Bill>();
    public DbSet<BillItem>         BillItems        => Set<BillItem>();
    public DbSet<BillAdjustment>   BillAdjustments  => Set<BillAdjustment>();
    public DbSet<Payment>          Payments         => Set<Payment>();
    public DbSet<PatientDocument>  PatientDocuments => Set<PatientDocument>();
    public DbSet<AuditLog>         AuditLogs           => Set<AuditLog>();
    public DbSet<ImagingProcedure> ImagingProcedures   => Set<ImagingProcedure>();
    public DbSet<RadiologyOrder>   RadiologyOrders     => Set<RadiologyOrder>();
    public DbSet<RadiologyOrderItem> RadiologyOrderItems => Set<RadiologyOrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.Entity<User>()           .HasQueryFilter(u => u.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<Patient>()        .HasQueryFilter(p => p.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<Appointment>()    .HasQueryFilter(a => a.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<Bill>()           .HasQueryFilter(b => b.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<BillItem>()       .HasQueryFilter(i => i.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<BillAdjustment>() .HasQueryFilter(a => a.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<Payment>()        .HasQueryFilter(p => p.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<PatientDocument>().HasQueryFilter(d => d.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<LabResult>()      .HasQueryFilter(r => r.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<LabObservation>() .HasQueryFilter(o => o.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<LabOrder>()       .HasQueryFilter(o => o.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<LabOrderItem>()   .HasQueryFilter(i => i.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<FacilitySettings>().HasQueryFilter(f => f.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<AuditLog>()          .HasQueryFilter(a => a.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<RadiologyOrder>()    .HasQueryFilter(o => o.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<RadiologyOrderItem>().HasQueryFilter(i => i.TenantId == _tenantContext.TenantId);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<TenantEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.TenantId  = _tenantContext.TenantId;
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
