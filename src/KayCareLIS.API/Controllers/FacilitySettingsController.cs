using KayCareLIS.Core.Constants;
using KayCareLIS.Core.DTOs.Facility;
using KayCareLIS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KayCareLIS.API.Controllers;

[ApiController]
[Route("api/facility-settings")]
[Authorize]
public class FacilitySettingsController : ControllerBase
{
    private readonly IFacilitySettingsService _facility;

    public FacilitySettingsController(IFacilitySettingsService facility) => _facility = facility;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var settings = await _facility.GetAsync(ct);
        return settings == null ? NotFound() : Ok(settings);
    }

    [HttpPut]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
    public async Task<IActionResult> Upsert([FromBody] SaveFacilitySettingsRequest request, CancellationToken ct)
        => Ok(await _facility.UpsertAsync(request, ct));

    [HttpPost("logo")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
    public async Task<IActionResult> UploadLogo(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0) return BadRequest(new { error = "No file provided." });
        if (file.Length > 2 * 1024 * 1024) return BadRequest(new { error = "Logo must be under 2 MB." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".png" && ext != ".jpg" && ext != ".jpeg")
            return BadRequest(new { error = "Only PNG and JPEG files are accepted." });

        await using var stream = file.OpenReadStream();
        var result = await _facility.UploadLogoAsync(stream, file.ContentType, ext, ct);
        return Ok(result);
    }

    [HttpDelete("logo")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
    public async Task<IActionResult> DeleteLogo(CancellationToken ct)
        => Ok(await _facility.DeleteLogoAsync(ct));
}
