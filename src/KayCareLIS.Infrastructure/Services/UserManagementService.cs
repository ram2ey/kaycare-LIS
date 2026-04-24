using KayCareLIS.Core.DTOs.Users;
using KayCareLIS.Core.Entities;
using KayCareLIS.Core.Exceptions;
using KayCareLIS.Core.Interfaces;
using KayCareLIS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KayCareLIS.Infrastructure.Services;

public class UserManagementService : IUserManagementService
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenantContext;

    public UserManagementService(AppDbContext db, ITenantContext tenantContext)
    {
        _db            = db;
        _tenantContext = tenantContext;
    }

    public async Task<List<UserResponse>> GetAllAsync(bool includeInactive = false, string? role = null, CancellationToken ct = default)
    {
        var query = _db.Users.Include(u => u.Role).AsNoTracking().AsQueryable();
        if (!includeInactive) query = query.Where(u => u.IsActive);
        if (!string.IsNullOrEmpty(role)) query = query.Where(u => u.Role!.RoleName == role);
        var users = await query.OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ToListAsync(ct);
        return users.Select(Map).ToList();
    }

    public async Task<UserResponse> GetByIdAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.Include(u => u.Role).AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId, ct)
            ?? throw new NotFoundException("User not found.");
        return Map(user);
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var email = request.Email.ToLower().Trim();
        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            throw new ConflictException($"A user with email '{email}' already exists.");

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleId == request.RoleId, ct)
            ?? throw new AppException($"Role '{request.RoleId}' not found.");

        var user = new User
        {
            UserId             = Guid.NewGuid(),
            TenantId           = _tenantContext.TenantId,
            RoleId             = role.RoleId,
            Email              = email,
            PasswordHash       = BCrypt.Net.BCrypt.HashPassword(request.Password, 12),
            FirstName          = request.FirstName.Trim(),
            LastName           = request.LastName.Trim(),
            PhoneNumber        = request.PhoneNumber?.Trim(),
            LicenseNumber      = request.LicenseNumber?.Trim(),
            Department         = request.Department?.Trim(),
            IsActive           = true,
            MustChangePassword = true,
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        user.Role = role;
        return Map(user);
    }

    public async Task<UserResponse> UpdateAsync(Guid userId, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId, ct)
            ?? throw new NotFoundException("User not found.");

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleId == request.RoleId, ct)
            ?? throw new AppException($"Role '{request.RoleId}' not found.");

        user.RoleId        = role.RoleId;
        user.FirstName     = request.FirstName.Trim();
        user.LastName      = request.LastName.Trim();
        user.PhoneNumber   = request.PhoneNumber?.Trim();
        user.LicenseNumber = request.LicenseNumber?.Trim();
        user.Department    = request.Department?.Trim();
        user.Role          = role;

        await _db.SaveChangesAsync(ct);
        return Map(user);
    }

    public async Task DeactivateAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId, ct)
            ?? throw new NotFoundException("User not found.");
        user.IsActive = false;
        await _db.SaveChangesAsync(ct);
    }

    public async Task ReactivateAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId, ct)
            ?? throw new NotFoundException("User not found.");
        user.IsActive = true;
        await _db.SaveChangesAsync(ct);
    }

    public async Task ResetPasswordAsync(Guid userId, ResetPasswordRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId, ct)
            ?? throw new NotFoundException("User not found.");
        user.PasswordHash       = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, 12);
        user.MustChangePassword = true;
        user.FailedLoginCount   = 0;
        user.LockedUntil        = null;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<DepartmentSummary>> GetDepartmentsAsync(CancellationToken ct = default)
    {
        var groups = await _db.Users
            .Where(u => u.IsActive && u.Department != null)
            .GroupBy(u => u.Department!)
            .Select(g => new DepartmentSummary { Name = g.Key, UserCount = g.Count() })
            .OrderBy(d => d.Name)
            .ToListAsync(ct);
        return groups;
    }

    public async Task RenameDepartmentAsync(RenameDepartmentRequest request, CancellationToken ct = default)
    {
        var users = await _db.Users.Where(u => u.Department == request.OldName).ToListAsync(ct);
        foreach (var u in users) u.Department = request.NewName;
        await _db.SaveChangesAsync(ct);
    }

    private static UserResponse Map(User u) => new()
    {
        UserId        = u.UserId,
        Email         = u.Email,
        FullName      = $"{u.FirstName} {u.LastName}",
        FirstName     = u.FirstName,
        LastName      = u.LastName,
        Role          = u.Role?.RoleName ?? string.Empty,
        PhoneNumber   = u.PhoneNumber,
        LicenseNumber = u.LicenseNumber,
        Department    = u.Department,
        IsActive      = u.IsActive,
        MustChangePassword = u.MustChangePassword,
    };
}
