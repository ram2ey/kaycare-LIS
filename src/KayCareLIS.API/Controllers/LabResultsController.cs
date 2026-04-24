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
}
