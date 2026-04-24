using KayCareLIS.Core.Interfaces;
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
            ?? "Server=.\\SQLEXPRESS;Database=KayCareLISDb;Trusted_Connection=True;TrustServerCertificate=True;";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        var tenantContext = new DesignTimeTenantContext();
        return new AppDbContext(optionsBuilder.Options, tenantContext);
    }
}

public class DesignTimeTenantContext : ITenantContext
{
    public Guid   TenantId   { get; set; } = Guid.Empty;
    public string TenantCode { get; set; } = "design";
}
