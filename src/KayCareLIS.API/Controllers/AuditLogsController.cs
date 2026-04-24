using KayCareLIS.Core.Constants;
using KayCareLIS.Core.DTOs.Audit;
using KayCareLIS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KayCareLIS.API.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
public class AuditLogsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AuditLogsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Query([FromQuery] AuditLogQueryRequest request, CancellationToken ct)
    {
        var query = _db.AuditLogs.AsNoTracking().AsQueryable();

        if (request.PatientId.HasValue)
            query = query.Where(l => l.PatientId == request.PatientId);

        if (request.UserId.HasValue)
            query = query.Where(l => l.UserId == request.UserId);

        if (!string.IsNullOrEmpty(request.Action))
            query = query.Where(l => l.Action == request.Action);

        if (request.From.HasValue)
            query = query.Where(l => l.Timestamp >= request.From.Value);

        if (request.To.HasValue)
            query = query.Where(l => l.Timestamp <= request.To.Value);

        var total = await query.CountAsync(ct);
        var page  = Math.Max(request.Page, 1);
        var size  = Math.Min(request.PageSize > 0 ? request.PageSize : 50, 200);

        var items = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(l => new AuditLogResponse
            {
                AuditLogId  = l.AuditLogId,
                TenantId    = l.TenantId,
                UserId      = l.UserId,
                UserEmail   = l.UserEmail,
                Action      = l.Action,
                EntityType  = l.EntityType,
                EntityId    = l.EntityId,
                PatientId   = l.PatientId,
                Details     = l.Details,
                IpAddress   = l.IpAddress,
                Timestamp   = l.Timestamp,
            })
            .ToListAsync(ct);

        return Ok(new { total, page, pageSize = size, items });
    }
}
