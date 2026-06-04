using KayCareLIS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KayCareLIS.API.Controllers;

[ApiController]
[Route("api/lab-results")]
[Authorize]
public class LabResultsController : ControllerBase
{
    private readonly ILabResultService _labResults;

    public LabResultsController(ILabResultService labResults) => _labResults = labResults;

    [HttpGet("patient/{patientId:guid}")]
    public async Task<IActionResult> GetByPatient(Guid patientId, CancellationToken ct)
        => Ok(await _labResults.GetByPatientAsync(patientId, ct));

    [HttpGet("accession/{accessionNumber}")]
    public async Task<IActionResult> GetByAccession(string accessionNumber, CancellationToken ct)
    {
        var result = await _labResults.GetByAccessionAsync(accessionNumber, ct);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _labResults.GetByIdAsync(id, ct);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost("simulate")]
    public async Task<IActionResult> SimulateHl7([FromBody] SimulateHl7Request request, CancellationToken ct)
    {
        var success = await _labResults.ProcessHl7MessageAsync(request.RawHl7, ct);
        if (!success)
        {
            return BadRequest(new { error = "Failed to parse HL7 message or duplicate accession." });
        }
        return Ok(new { success = true });
    }
}

public class SimulateHl7Request
{
    public string RawHl7 { get; set; } = string.Empty;
}
