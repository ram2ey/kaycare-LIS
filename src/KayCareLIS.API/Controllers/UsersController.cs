using KayCareLIS.Core.Constants;
using KayCareLIS.Core.DTOs.Users;
using KayCareLIS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KayCareLIS.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserManagementService _users;

    public UsersController(IUserManagementService users) => _users = users;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool includeInactive = false,
        [FromQuery] string? role = null,
        CancellationToken ct = default)
    {
        var list = await _users.GetAllAsync(includeInactive, role, ct);
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await _users.GetByIdAsync(id, ct));

    [HttpPost]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var user = await _users.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = user.UserId }, user);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken ct)
        => Ok(await _users.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await _users.DeactivateAsync(id, ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/reactivate")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
    public async Task<IActionResult> Reactivate(Guid id, CancellationToken ct)
    {
        await _users.ReactivateAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/reset-password")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        await _users.ResetPasswordAsync(id, request, ct);
        return NoContent();
    }

    [HttpGet("departments")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
    public async Task<IActionResult> GetDepartments(CancellationToken ct)
        => Ok(await _users.GetDepartmentsAsync(ct));

    [HttpPut("departments/rename")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
    public async Task<IActionResult> RenameDepartment([FromBody] RenameDepartmentRequest request, CancellationToken ct)
    {
        await _users.RenameDepartmentAsync(request, ct);
        return NoContent();
    }
}
