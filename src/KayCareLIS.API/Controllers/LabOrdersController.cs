using KayCareLIS.Core.Constants;
using KayCareLIS.Core.DTOs.LabOrders;
using KayCareLIS.Core.Interfaces;
using KayCareLIS.Core.Exceptions;
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
    public async Task<IActionResult> GetCatalog([FromQuery] bool includeInactive, CancellationToken ct)
        => Ok(await _labOrders.GetTestCatalogAsync(includeInactive, ct));

    [HttpPost("catalog")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> CreateCatalogItem([FromBody] CreateLabTestCatalogRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _labOrders.CreateTestCatalogItemAsync(req, ct);
            return CreatedAtAction(nameof(GetCatalog), new { includeInactive = true }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("catalog/{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> UpdateCatalogItem(Guid id, [FromBody] UpdateLabTestCatalogRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _labOrders.UpdateTestCatalogItemAsync(id, req, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("catalog/{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> DeleteCatalogItem(Guid id, CancellationToken ct)
    {
        try
        {
            await _labOrders.DeleteTestCatalogItemAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

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

    [HttpGet("critical-alerts")]
    public async Task<IActionResult> GetCriticalAlerts(CancellationToken ct)
        => Ok(await _labOrders.GetCriticalAlertsAsync(ct));

    [HttpPost("items/{itemId:guid}/critical-log")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.LabTechnician},{Roles.Doctor}")]
    public async Task<IActionResult> RecordCriticalCallLog(Guid itemId, [FromBody] CreateCriticalCallLogRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _labOrders.RecordCriticalCallLogAsync(itemId, request, ct);
            return Ok(result);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
