using KayCareLIS.Core.Constants;
using KayCareLIS.Core.DTOs.Radiology;
using KayCareLIS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KayCareLIS.API.Controllers;

[ApiController]
[Route("api/radiology-orders")]
[Authorize]
public class RadiologyOrdersController : ControllerBase
{
    private readonly IRadiologyOrderService  _radiology;
    private readonly IRadiologyReportService _radiologyReport;

    public RadiologyOrdersController(IRadiologyOrderService radiology, IRadiologyReportService radiologyReport)
    {
        _radiology       = radiology;
        _radiologyReport = radiologyReport;
    }

    [HttpGet("catalog")]
    public async Task<IActionResult> GetCatalog(CancellationToken ct)
        => Ok(await _radiology.GetProcedureCatalogAsync(ct));

    [HttpGet("worklist")]
    public async Task<IActionResult> GetWorklist(
        [FromQuery] DateOnly? date,
        [FromQuery] string?   status,
        CancellationToken ct)
    {
        var d = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return Ok(await _radiology.GetWorklistAsync(d, status, ct));
    }

    [HttpGet("patient/{patientId:guid}")]
    public async Task<IActionResult> GetByPatient(Guid patientId, CancellationToken ct)
        => Ok(await _radiology.GetByPatientAsync(patientId, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var order = await _radiology.GetByIdAsync(id, ct);
        return order == null ? NotFound() : Ok(order);
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Doctor},{Roles.Receptionist}")]
    public async Task<IActionResult> PlaceOrder([FromBody] CreateRadiologyOrderRequest request, CancellationToken ct)
    {
        var order = await _radiology.PlaceOrderAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = order.RadiologyOrderId }, order);
    }

    [HttpPost("items/{itemId:guid}/acquire")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.LabTechnician}")]
    public async Task<IActionResult> MarkAcquired(Guid itemId, CancellationToken ct)
        => Ok(await _radiology.MarkAcquiredAsync(itemId, ct));

    [HttpPost("items/{itemId:guid}/report")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Doctor}")]
    public async Task<IActionResult> EnterReport(Guid itemId, [FromBody] RadiologyReportRequest request, CancellationToken ct)
        => Ok(await _radiology.EnterReportAsync(itemId, request, ct));

    [HttpPost("items/{itemId:guid}/sign")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Doctor}")]
    public async Task<IActionResult> SignItem(Guid itemId, CancellationToken ct)
        => Ok(await _radiology.SignItemAsync(itemId, ct));

    [HttpGet("{id:guid}/report")]
    public async Task<IActionResult> GetReport(Guid id, CancellationToken ct)
    {
        var pdf = await _radiologyReport.GenerateReportAsync(id, ct);
        if (pdf == null) return NotFound();
        return File(pdf, "application/pdf", $"radiology-report-{id:N}.pdf");
    }
}
