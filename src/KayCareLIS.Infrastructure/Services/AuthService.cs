using KayCareLIS.Core.DTOs.Auth;
using KayCareLIS.Core.Entities;
using KayCareLIS.Core.Exceptions;
using KayCareLIS.Core.Interfaces;
using KayCareLIS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KayCareLIS.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext    _db;
    private readonly ITokenService  _token;
    private readonly ITenantContext _tenantContext;

    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(30);

    public AuthService(AppDbContext db, ITokenService token, ITenantContext tenantContext)
    {
        _db            = db;
        _token         = token;
        _tenantContext = tenantContext;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower().Trim(), ct)
            ?? throw new AppException("Invalid email or password.");

        if (user.LockedUntil.HasValue && user.LockedUntil > DateTime.UtcNow)
            throw new AppException($"Account locked. Try again after {user.LockedUntil:HH:mm} UTC.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            user.FailedLoginCount++;
            if (user.FailedLoginCount >= MaxFailedAttempts)
            {
                user.LockedUntil = DateTime.UtcNow.Add(LockoutDuration);
                user.FailedLoginCount = 0;
            }
            await _db.SaveChangesAsync(ct);
            throw new AppException("Invalid email or password.");
        }

        if (!user.IsActive)
            throw new AppException("Account is deactivated. Contact your administrator.");

        user.FailedLoginCount = 0;
        user.LockedUntil      = null;
        await _db.SaveChangesAsync(ct);

        var token = _token.GenerateToken(user, user.Role!.RoleName);
        return new LoginResponse
        {
            Token              = token,
            UserId             = user.UserId.ToString(),
            Email              = user.Email,
            FullName           = $"{user.FirstName} {user.LastName}",
            Role               = user.Role.RoleName,
            MustChangePassword = user.MustChangePassword,
        };
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId, ct)
            ?? throw new NotFoundException("User not found.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            throw new AppException("Current password is incorrect.");

        user.PasswordHash      = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, 12);
        user.MustChangePassword = false;
        await _db.SaveChangesAsync(ct);
    }
}
