using KayCareLIS.Core.Constants;
using KayCareLIS.Core.DTOs.LabOrders;
using KayCareLIS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KayCareLIS.API.Controllers;

[ApiController]
[Route("api/lab-orders")]
[Authorize]
public class LabOrdersController : ControllerBase
{
    private readonly ILabOrderService  _labOrders;
    private readonly ILabReportService _labReport;

    public LabOrdersController(ILabOrderService labOrders, ILabReportService labReport)
    {
        _labOrders = labOrders;
        _labReport = labReport;
    }

    [HttpGet("catalog")]
    public async Task<IActionResult> GetCatalog(CancellationToken ct)
        => Ok(await _labOrders.GetTestCatalogAsync(ct));

    [HttpGet("waiting-list")]
    public async Task<IActionResult> GetWaitingList(
        [FromQuery] DateOnly? date,
        [FromQuery] string?   status,
        [FromQuery] string?   department,
        CancellationToken ct)
    {
        var d = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return Ok(await _labOrders.GetWaitingListAsync(d, status, department, ct));
    }

    [HttpGet("patient/{patientId:guid}")]
    public async Task<IActionResult> GetByPatient(Guid patientId, CancellationToken ct)
        => Ok(await _labOrders.GetByPatientAsync(patientId, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var order = await _labOrders.GetByIdAsync(id, ct);
        return order == null ? NotFound() : Ok(order);
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Doctor},{Roles.Receptionist}")]
    public async Task<IActionResult> PlaceOrder([FromBody] CreateLabOrderRequest request, CancellationToken ct)
    {
        var order = await _labOrders.PlaceOrderAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = order.LabOrderId }, order);
    }

    [HttpPost("items/{itemId:guid}/receive-sample")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.LabTechnician}")]
    public async Task<IActionResult> ReceiveSample(Guid itemId, CancellationToken ct)
        => Ok(await _labOrders.ReceiveSampleAsync(itemId, ct));

    [HttpPost("items/{itemId:guid}/result")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.LabTechnician},{Roles.Doctor}")]
    public async Task<IActionResult> EnterResult(Guid itemId, [FromBody] ManualResultRequest request, CancellationToken ct)
        => Ok(await _labOrders.EnterManualResultAsync(itemId, request, ct));

    [HttpPost("items/{itemId:guid}/sign")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Doctor}")]
    public async Task<IActionResult> SignItem(Guid itemId, CancellationToken ct)
        => Ok(await _labOrders.SignItemAsync(itemId, ct));

    [HttpGet("{id:guid}/report")]
    public async Task<IActionResult> GetReport(Guid id, CancellationToken ct)
    {
        var pdf = await _labReport.GenerateLabOrderReportAsync(id, ct);
        if (pdf == null) return NotFound();
        return File(pdf, "application/pdf", $"lab-report-{id:N}.pdf");
    }
}
