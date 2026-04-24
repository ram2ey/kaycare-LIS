using KayCareLIS.Core.DTOs.IcdCodes;
using KayCareLIS.Core.Interfaces;
using KayCareLIS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KayCareLIS.Infrastructure.Services;

public class IcdCodeService : IIcdCodeService
{
    private readonly AppDbContext _db;

    public IcdCodeService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<IcdCodeResponse>> SearchAsync(string query, int limit, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return [];

        var q = query.Trim();

        var results = await _db.IcdCodes
            .AsNoTracking()
            .Where(c => EF.Functions.Like(c.Code, $"{q}%") || EF.Functions.Like(c.Description, $"%{q}%"))
            .OrderBy(c => EF.Functions.Like(c.Code, $"{q}%") ? 0 : 1)
            .ThenBy(c => c.Code)
            .Take(limit > 0 ? limit : 20)
            .ToListAsync(ct);

        return results.Select(c => new IcdCodeResponse(c.Code, c.Description, c.Chapter ?? string.Empty)).ToList();
    }
}
