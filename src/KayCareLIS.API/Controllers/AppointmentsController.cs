using KayCareLIS.Core.Constants;
using KayCareLIS.Core.DTOs.Appointments;
using KayCareLIS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KayCareLIS.API.Controllers;

[ApiController]
[Route("api/appointments")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointments;

    public AppointmentsController(IAppointmentService appointments) => _appointments = appointments;

    [HttpGet("calendar")]
    public async Task<IActionResult> GetCalendar(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid? doctorUserId,
        [FromQuery] string? status,
        CancellationToken ct)
    {
        var req = new CalendarRequest { From = from, To = to, DoctorUserId = doctorUserId, Status = status };
        return Ok(await _appointments.GetCalendarAsync(req, ct));
    }

    [HttpGet("patient/{patientId:guid}")]
    public async Task<IActionResult> GetByPatient(Guid patientId, CancellationToken ct)
        => Ok(await _appointments.GetPatientAppointmentsAsync(patientId, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await _appointments.GetByIdAsync(id, ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentRequest request, CancellationToken ct)
    {
        var appt = await _appointments.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = appt.AppointmentId }, appt);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAppointmentRequest request, CancellationToken ct)
        => Ok(await _appointments.UpdateAsync(id, request, ct));

    [HttpPost("{id:guid}/confirm")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Doctor},{Roles.Receptionist}")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct)
        => Ok(await _appointments.TransitionStatusAsync(id, AppointmentStatus.Confirmed, null, ct));

    [HttpPost("{id:guid}/check-in")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Receptionist}")]
    public async Task<IActionResult> CheckIn(Guid id, CancellationToken ct)
        => Ok(await _appointments.TransitionStatusAsync(id, AppointmentStatus.CheckedIn, null, ct));

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelAppointmentRequest request, CancellationToken ct)
        => Ok(await _appointments.TransitionStatusAsync(id, AppointmentStatus.Cancelled, request.Reason, ct));

    [HttpPost("{id:guid}/no-show")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Receptionist}")]
    public async Task<IActionResult> NoShow(Guid id, CancellationToken ct)
        => Ok(await _appointments.TransitionStatusAsync(id, AppointmentStatus.NoShow, null, ct));
}
