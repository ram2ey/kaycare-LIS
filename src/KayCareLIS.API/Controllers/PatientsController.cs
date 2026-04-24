using KayCareLIS.Core.DTOs.Patients;
using KayCareLIS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KayCareLIS.API.Controllers;

[ApiController]
[Route("api/patients")]
[Authorize]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _patients;

    public PatientsController(IPatientService patients) => _patients = patients;

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] PatientSearchRequest request, CancellationToken ct)
        => Ok(await _patients.SearchAsync(request, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await _patients.GetByIdAsync(id, ct));

    [HttpPost]
    public async Task<IActionResult> Register([FromBody] CreatePatientRequest request, CancellationToken ct)
    {
        var patient = await _patients.RegisterAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = patient.PatientId }, patient);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePatientRequest request, CancellationToken ct)
        => Ok(await _patients.UpdateAsync(id, request, ct));
}
