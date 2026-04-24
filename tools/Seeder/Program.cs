using KayCareLIS.Core.Entities;
using KayCareLIS.Infrastructure.Data;
using KayCareLIS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var connectionString = args.FirstOrDefault()
    ?? "Server=.\\SQLEXPRESS;Database=KayCareLISDb;Trusted_Connection=True;TrustServerCertificate=True;";

Console.WriteLine($"Seeding KayCareLISDb...");

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlServer(connectionString, o => o.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null))
    .Options;

var tenantContext = new DesignTimeTenantContext();
await using var db = new AppDbContext(options, tenantContext);

await db.Database.MigrateAsync();
Console.WriteLine("Migrations applied.");

// ── Roles ──────────────────────────────────────────────────
var roleNames = new[] { "SuperAdmin", "Admin", "Doctor", "LabTechnician", "Receptionist", "BillingOfficer" };
for (int i = 0; i < roleNames.Length; i++)
{
    if (!await db.Roles.AnyAsync(r => r.RoleName == roleNames[i]))
    {
        db.Roles.Add(new Role { RoleId = i + 1, RoleName = roleNames[i], Description = roleNames[i] });
        Console.WriteLine($"  + Role: {roleNames[i]}");
    }
}
await db.SaveChangesAsync();

// ── Demo Tenant ─────────────────────────────────────────────
var tenantCode = "demo";
var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.TenantCode == tenantCode);
if (tenant == null)
{
    tenant = new Tenant
    {
        TenantId         = Guid.NewGuid(),
        TenantCode       = tenantCode,
        TenantName       = "KayCare Demo Lab",
        Subdomain        = tenantCode,
        SubscriptionPlan = "Standard",
        IsActive         = true,
        MaxUsers         = 50,
        StorageQuotaGB   = 10,
    };
    db.Tenants.Add(tenant);
    await db.SaveChangesAsync();
    Console.WriteLine($"  + Tenant: {tenant.TenantName} ({tenant.TenantCode})");
}
else
{
    Console.WriteLine($"  = Tenant already exists: {tenant.TenantName}");
}

tenantContext.TenantId   = tenant.TenantId;
tenantContext.TenantCode = tenant.TenantCode;

// ── Admin User ──────────────────────────────────────────────
var adminEmail = "admin@demo.com";
if (!await db.Users.AnyAsync(u => u.Email == adminEmail))
{
    var adminRole = await db.Roles.FirstAsync(r => r.RoleName == "Admin");
    db.Users.Add(new User
    {
        UserId             = Guid.NewGuid(),
        TenantId           = tenant.TenantId,
        RoleId             = adminRole.RoleId,
        Email              = adminEmail,
        PasswordHash       = BCrypt.Net.BCrypt.HashPassword("Admin@1234", 12),
        FirstName          = "Admin",
        LastName           = "User",
        IsActive           = true,
        MustChangePassword = false,
    });
    await db.SaveChangesAsync();
    Console.WriteLine($"  + Admin user: {adminEmail} / Admin@1234");
}
else
{
    Console.WriteLine($"  = Admin user already exists: {adminEmail}");
}

var testCount = await db.LabTestCatalog.CountAsync();
Console.WriteLine($"  = Lab test catalog: {testCount} tests (seeded by EF migration).");

Console.WriteLine();
Console.WriteLine("Seed complete.");
Console.WriteLine();
Console.WriteLine("  Tenant:   demo");
Console.WriteLine("  Email:    admin@demo.com");
Console.WriteLine("  Password: Admin@1234");
Console.WriteLine();
Console.WriteLine("Run the API with X-Tenant-Code: demo header for local dev.");
