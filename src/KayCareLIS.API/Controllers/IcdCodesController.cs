using KayCareLIS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KayCareLIS.API.Controllers;

[ApiController]
[Route("api/icd-codes")]
[Authorize]
public class IcdCodesController : ControllerBase
{
    private readonly IIcdCodeService _icdCodes;

    public IcdCodesController(IIcdCodeService icdCodes) => _icdCodes = icdCodes;

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? q, [FromQuery] int limit = 20, CancellationToken ct = default)
        => Ok(await _icdCodes.SearchAsync(q ?? string.Empty, limit, ct));
}
