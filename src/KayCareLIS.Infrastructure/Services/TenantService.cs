using KayCareLIS.Core.DTOs.Tenants;
using KayCareLIS.Core.Entities;
using KayCareLIS.Core.Exceptions;
using KayCareLIS.Core.Interfaces;
using KayCareLIS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KayCareLIS.Infrastructure.Services;

public class TenantService : ITenantService
{
    private readonly AppDbContext _db;

    public TenantService(AppDbContext db) => _db = db;

    public async Task<List<TenantResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var tenants = await _db.Tenants.AsNoTracking().OrderBy(t => t.TenantName).ToListAsync(ct);
        return tenants.Select(Map).ToList();
    }

    public async Task<TenantResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var t = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(x => x.TenantId == id, ct);
        return t == null ? null : Map(t);
    }

    public async Task<TenantResponse> CreateAsync(CreateTenantRequest req, CancellationToken ct = default)
    {
        if (await _db.Tenants.AnyAsync(t => t.TenantCode == req.TenantCode.ToLower(), ct))
            throw new ConflictException($"Tenant code '{req.TenantCode}' already exists.");

        var tenant = new Tenant
        {
            TenantId         = Guid.NewGuid(),
            TenantCode       = req.TenantCode.ToLower().Trim(),
            TenantName       = req.TenantName.Trim(),
            Subdomain        = req.TenantCode.ToLower().Trim(),
            SubscriptionPlan = string.IsNullOrEmpty(req.SubscriptionPlan) ? "Standard" : req.SubscriptionPlan,
            IsActive         = true,
            MaxUsers         = req.MaxUsers > 0 ? req.MaxUsers : 50,
            StorageQuotaGB   = req.StorageQuotaGB > 0 ? req.StorageQuotaGB : 10,
        };
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(ct);
        return Map(tenant);
    }

    public async Task<TenantResponse> UpdateAsync(Guid id, UpdateTenantRequest req, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.TenantId == id, ct)
            ?? throw new NotFoundException("Tenant not found.");

        tenant.TenantName       = req.TenantName.Trim();
        tenant.SubscriptionPlan = req.SubscriptionPlan;
        tenant.MaxUsers         = req.MaxUsers;
        tenant.StorageQuotaGB   = req.StorageQuotaGB;
        await _db.SaveChangesAsync(ct);
        return Map(tenant);
    }

    public async Task<TenantResponse> SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.TenantId == id, ct)
            ?? throw new NotFoundException("Tenant not found.");
        tenant.IsActive = isActive;
        await _db.SaveChangesAsync(ct);
        return Map(tenant);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.TenantId == id, ct)
            ?? throw new NotFoundException("Tenant not found.");
        _db.Tenants.Remove(tenant);
        await _db.SaveChangesAsync(ct);
    }

    private static TenantResponse Map(Tenant t) => new()
    {
        TenantId         = t.TenantId,
        TenantCode       = t.TenantCode,
        TenantName       = t.TenantName,
        Subdomain        = t.Subdomain,
        SubscriptionPlan = t.SubscriptionPlan,
        IsActive         = t.IsActive,
        MaxUsers         = t.MaxUsers,
        StorageQuotaGB   = t.StorageQuotaGB,
    };
}
