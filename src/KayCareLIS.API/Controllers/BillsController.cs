using KayCareLIS.Core.Constants;
using KayCareLIS.Core.DTOs.Billing;
using KayCareLIS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KayCareLIS.API.Controllers;

[ApiController]
[Route("api/bills")]
[Authorize]
public class BillsController : ControllerBase
{
    private readonly IBillingService    _billing;
    private readonly IBillingPdfService _billingPdf;

    public BillsController(IBillingService billing, IBillingPdfService billingPdf)
    {
        _billing    = billing;
        _billingPdf = billingPdf;
    }

    [HttpGet("patient/{patientId:guid}")]
    public async Task<IActionResult> GetByPatient(Guid patientId, CancellationToken ct)
        => Ok(await _billing.GetPatientBillsAsync(patientId, ct));

    [HttpGet("outstanding")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.BillingOfficer}")]
    public async Task<IActionResult> GetOutstanding(CancellationToken ct)
        => Ok(await _billing.GetOutstandingAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await _billing.GetByIdAsync(id, ct));

    [HttpPost]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Receptionist},{Roles.BillingOfficer}")]
    public async Task<IActionResult> Create([FromBody] CreateBillRequest request, CancellationToken ct)
    {
        var bill = await _billing.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = bill.BillId }, bill);
    }

    [HttpPost("{id:guid}/issue")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Receptionist},{Roles.BillingOfficer}")]
    public async Task<IActionResult> Issue(Guid id, CancellationToken ct)
        => Ok(await _billing.IssueAsync(id, ct));

    [HttpPost("{id:guid}/payment")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Receptionist},{Roles.BillingOfficer}")]
    public async Task<IActionResult> AddPayment(Guid id, [FromBody] AddPaymentRequest request, CancellationToken ct)
        => Ok(await _billing.AddPaymentAsync(id, request, ct));

    [HttpPost("{id:guid}/discount")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
    public async Task<IActionResult> ApplyDiscount(Guid id, [FromBody] ApplyDiscountRequest request, CancellationToken ct)
        => Ok(await _billing.ApplyDiscountAsync(id, request, ct));

    [HttpPost("{id:guid}/adjustment")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
    public async Task<IActionResult> AddAdjustment(Guid id, [FromBody] AddAdjustmentRequest request, CancellationToken ct)
        => Ok(await _billing.AddAdjustmentAsync(id, request, ct));

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
        => Ok(await _billing.CancelAsync(id, ct));

    [HttpPost("{id:guid}/void")]
    [Authorize(Roles = $"{Roles.SuperAdmin}")]
    public async Task<IActionResult> Void(Guid id, CancellationToken ct)
        => Ok(await _billing.VoidAsync(id, ct));

    [HttpGet("{id:guid}/report")]
    public async Task<IActionResult> GetInvoicePdf(Guid id, CancellationToken ct)
    {
        var pdf = await _billingPdf.GenerateBillInvoiceAsync(id, ct);
        if (pdf == null) return NotFound();
        return File(pdf, "application/pdf", $"invoice-{id:N}.pdf");
    }

    [HttpGet("payments/{paymentId:guid}/receipt")]
    public async Task<IActionResult> GetReceiptPdf(Guid paymentId, CancellationToken ct)
    {
        var pdf = await _billingPdf.GeneratePaymentReceiptAsync(paymentId, ct);
        if (pdf == null) return NotFound();
        return File(pdf, "application/pdf", $"receipt-{paymentId:N}.pdf");
    }
}
