using KayCareLIS.Infrastructure.Data;
using KayCareLIS.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KayCareLIS.Infrastructure.Middleware;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IServiceProvider services)
    {
        var host = context.Request.Host.Host.ToLower();

        // Skip tenant resolution for Azure hosting domains
        if (host.EndsWith(".azurewebsites.net") || host.EndsWith(".azurestaticapps.net") ||
            host == "localhost" || host == "127.0.0.1")
        {
            // Use X-Tenant-Code header for local dev / API clients
            var headerCode = context.Request.Headers["X-Tenant-Code"].FirstOrDefault();
            if (!string.IsNullOrEmpty(headerCode))
                await ResolveTenantByCodeAsync(context, services, headerCode);

            await _next(context);
            return;
        }

        // Extract subdomain — first part of the host
        var parts = host.Split('.');
        if (parts.Length < 2)
        {
            await _next(context);
            return;
        }

        var subdomain = parts[0];
        await ResolveTenantBySubdomainAsync(context, services, subdomain);
        await _next(context);
    }

    private static async Task ResolveTenantBySubdomainAsync(HttpContext context, IServiceProvider services, string subdomain)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tenant = await db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Subdomain == subdomain && t.IsActive);

        if (tenant == null)
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsJsonAsync(new { error = "Tenant not found." });
            return;
        }

        SetTenantContext(context, services, tenant.TenantId, tenant.TenantCode);
    }

    private static async Task ResolveTenantByCodeAsync(HttpContext context, IServiceProvider services, string code)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tenant = await db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TenantCode == code.ToLower() && t.IsActive);

        if (tenant == null) return;

        SetTenantContext(context, services, tenant.TenantId, tenant.TenantCode);
    }

    private static void SetTenantContext(HttpContext context, IServiceProvider services, Guid tenantId, string tenantCode)
    {
        var tenantContext = context.RequestServices.GetRequiredService<TenantContext>();
        tenantContext.TenantId   = tenantId;
        tenantContext.TenantCode = tenantCode;
    }
}
