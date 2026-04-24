using KayCareLIS.Core.Constants;
using KayCareLIS.Core.DTOs.Tenants;
using KayCareLIS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KayCareLIS.API.Controllers;

[ApiController]
[Route("api/tenants")]
[Authorize(Roles = Roles.SuperAdmin)]
public class TenantsController : ControllerBase
{
    private readonly ITenantService _tenants;

    public TenantsController(ITenantService tenants) => _tenants = tenants;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await _tenants.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var t = await _tenants.GetByIdAsync(id, ct);
        return t == null ? NotFound() : Ok(t);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTenantRequest request, CancellationToken ct)
    {
        var t = await _tenants.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = t.TenantId }, t);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTenantRequest request, CancellationToken ct)
        => Ok(await _tenants.UpdateAsync(id, request, ct));

    [HttpPatch("{id:guid}/active")]
    public async Task<IActionResult> SetActive(Guid id, [FromQuery] bool value, CancellationToken ct)
        => Ok(await _tenants.SetActiveAsync(id, value, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _tenants.DeleteAsync(id, ct);
        return NoContent();
    }
}
