using KayCareLIS.Core.Interfaces;
using KayCareLIS.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace KayCareLIS.Infrastructure.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=kaycare_lis;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        var tenantContext = new DesignTimeTenantContext();
        var encryption = new EncryptionHelper(config);
        return new AppDbContext(optionsBuilder.Options, tenantContext, encryption);
    }
}

public class DesignTimeTenantContext : ITenantContext
{
    public Guid   TenantId   { get; set; } = Guid.Empty;
    public string TenantCode { get; set; } = "design";
}
