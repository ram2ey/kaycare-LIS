using KayCareLIS.Core.Constants;
using KayCareLIS.Core.DTOs.Billing;
using KayCareLIS.Core.Entities;
using KayCareLIS.Core.Exceptions;
using KayCareLIS.Core.Interfaces;
using KayCareLIS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KayCareLIS.Infrastructure.Services;

public class BillingService : IBillingService
{
    private readonly AppDbContext        _db;
    private readonly ITenantContext      _tenantContext;
    private readonly ICurrentUserService _currentUser;

    public BillingService(AppDbContext db, ITenantContext tenantContext, ICurrentUserService currentUser)
    {
        _db            = db;
        _tenantContext = tenantContext;
        _currentUser   = currentUser;
    }

    public async Task<BillDetailResponse> CreateAsync(CreateBillRequest request, CancellationToken ct = default)
    {
        var patient = await _db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.PatientId == request.PatientId, ct)
            ?? throw new NotFoundException("Patient not found.");

        var billNumber = await GenerateBillNumberAsync(ct);
        var bill = new Bill
        {
            BillId          = Guid.NewGuid(),
            BillNumber      = billNumber,
            PatientId       = request.PatientId,
            CreatedByUserId = _currentUser.UserId,
            Status          = BillStatus.Draft,
            Notes           = request.Notes?.Trim(),
        };
        _db.Bills.Add(bill);
        await _db.SaveChangesAsync(ct);

        foreach (var itemReq in request.Items)
        {
            var item = new BillItem
            {
                ItemId      = Guid.NewGuid(),
                TenantId    = _tenantContext.TenantId,
                BillId      = bill.BillId,
                Description = itemReq.Description.Trim(),
                Category    = itemReq.Category?.Trim(),
                Quantity    = itemReq.Quantity > 0 ? itemReq.Quantity : 1,
                UnitPrice   = itemReq.UnitPrice,
            };
            _db.BillItems.Add(item);
        }

        await _db.SaveChangesAsync(ct);
        await RecalculateTotalsAsync(bill.BillId, ct);
        return await GetByIdAsync(bill.BillId, ct);
    }

    public async Task<BillDetailResponse> GetByIdAsync(Guid billId, CancellationToken ct = default)
    {
        var bill = await LoadBill(billId, ct);
        return MapDetail(bill);
    }

    public async Task<IReadOnlyList<BillResponse>> GetPatientBillsAsync(Guid patientId, CancellationToken ct = default)
    {
        var bills = await _db.Bills
            .Include(b => b.Patient)
            .AsNoTracking()
            .Where(b => b.PatientId == patientId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(ct);
        return bills.Select(MapSummary).ToList();
    }

    public async Task<IReadOnlyList<BillResponse>> GetOutstandingAsync(CancellationToken ct = default)
    {
        var bills = await _db.Bills
            .Include(b => b.Patient)
            .AsNoTracking()
            .Where(b => b.Status == BillStatus.Issued || b.Status == BillStatus.PartiallyPaid)
            .OrderBy(b => b.IssuedAt)
            .ToListAsync(ct);
        return bills.Select(MapSummary).ToList();
    }

    public async Task<BillDetailResponse> IssueAsync(Guid billId, CancellationToken ct = default)
    {
        var bill = await _db.Bills.FirstOrDefaultAsync(b => b.BillId == billId, ct)
            ?? throw new NotFoundException("Bill not found.");

        if (bill.Status != BillStatus.Draft)
            throw new ConflictException("Only Draft bills can be issued.");

        bill.Status   = BillStatus.Issued;
        bill.IssuedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(billId, ct);
    }

    public async Task<BillDetailResponse> AddPaymentAsync(Guid billId, AddPaymentRequest request, CancellationToken ct = default)
    {
        var bill = await _db.Bills.FirstOrDefaultAsync(b => b.BillId == billId, ct)
            ?? throw new NotFoundException("Bill not found.");

        if (bill.Status == BillStatus.Paid || bill.Status == BillStatus.Cancelled || bill.Status == BillStatus.Void)
            throw new ConflictException($"Cannot add payment to a {bill.Status} bill.");

        if (request.Amount <= 0)
            throw new AppException("Payment amount must be greater than zero.");

        var payment = new Payment
        {
            PaymentId        = Guid.NewGuid(),
            BillId           = billId,
            Amount           = request.Amount,
            PaymentMethod    = request.PaymentMethod,
            Reference        = request.Reference?.Trim(),
            ReceivedByUserId = _currentUser.UserId,
            PaymentDate      = DateTime.UtcNow,
            Notes            = request.Notes?.Trim(),
        };
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync(ct);
        await RecalculateTotalsAsync(billId, ct);
        return await GetByIdAsync(billId, ct);
    }

    public async Task<BillDetailResponse> ApplyDiscountAsync(Guid billId, ApplyDiscountRequest request, CancellationToken ct = default)
    {
        var bill = await _db.Bills.FirstOrDefaultAsync(b => b.BillId == billId, ct)
            ?? throw new NotFoundException("Bill not found.");

        if (bill.Status == BillStatus.Paid || bill.Status == BillStatus.Cancelled || bill.Status == BillStatus.Void)
            throw new ConflictException($"Cannot apply discount to a {bill.Status} bill.");

        bill.DiscountAmount = request.DiscountAmount;
        bill.DiscountReason = request.DiscountReason?.Trim();
        await _db.SaveChangesAsync(ct);
        await RecalculateTotalsAsync(billId, ct);
        return await GetByIdAsync(billId, ct);
    }

    public async Task<BillDetailResponse> AddAdjustmentAsync(Guid billId, AddAdjustmentRequest request, CancellationToken ct = default)
    {
        var bill = await _db.Bills.FirstOrDefaultAsync(b => b.BillId == billId, ct)
            ?? throw new NotFoundException("Bill not found.");

        if (bill.Status == BillStatus.Paid || bill.Status == BillStatus.Cancelled || bill.Status == BillStatus.Void)
            throw new ConflictException($"Cannot adjust a {bill.Status} bill.");

        var adj = new BillAdjustment
        {
            BillAdjustmentId = Guid.NewGuid(),
            BillId           = billId,
            TenantId         = _tenantContext.TenantId,
            Amount           = request.Amount,
            Reason           = request.Reason.Trim(),
            AdjustedByUserId = _currentUser.UserId,
            AdjustedAt       = DateTime.UtcNow,
        };
        _db.BillAdjustments.Add(adj);
        await _db.SaveChangesAsync(ct);
        await RecalculateTotalsAsync(billId, ct);
        return await GetByIdAsync(billId, ct);
    }

    public async Task<BillDetailResponse> CancelAsync(Guid billId, CancellationToken ct = default)
    {
        var bill = await _db.Bills.FirstOrDefaultAsync(b => b.BillId == billId, ct)
            ?? throw new NotFoundException("Bill not found.");

        if (bill.Status == BillStatus.Paid || bill.Status == BillStatus.Void)
            throw new ConflictException($"Cannot cancel a {bill.Status} bill.");

        bill.Status = BillStatus.Cancelled;
        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(billId, ct);
    }

    public async Task<BillDetailResponse> VoidAsync(Guid billId, CancellationToken ct = default)
    {
        var bill = await _db.Bills.FirstOrDefaultAsync(b => b.BillId == billId, ct)
            ?? throw new NotFoundException("Bill not found.");

        bill.Status = BillStatus.Void;
        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(billId, ct);
    }

    private async Task RecalculateTotalsAsync(Guid billId, CancellationToken ct)
    {
        var bill = await _db.Bills.FirstOrDefaultAsync(b => b.BillId == billId, ct);
        if (bill == null) return;

        var items       = await _db.BillItems.Where(i => i.BillId == billId).ToListAsync(ct);
        var payments    = await _db.Payments.Where(p => p.BillId == billId).ToListAsync(ct);
        var adjustments = await _db.BillAdjustments.Where(a => a.BillId == billId).ToListAsync(ct);

        bill.TotalAmount     = items.Sum(i => i.Quantity * i.UnitPrice);
        bill.AdjustmentTotal = adjustments.Sum(a => a.Amount);
        bill.PaidAmount      = payments.Sum(p => p.Amount);

        var balance = bill.TotalAmount + bill.AdjustmentTotal - bill.DiscountAmount - bill.PaidAmount;

        if (bill.Status == BillStatus.Issued || bill.Status == BillStatus.PartiallyPaid)
        {
            if (balance <= 0)
                bill.Status = BillStatus.Paid;
            else if (bill.PaidAmount > 0)
                bill.Status = BillStatus.PartiallyPaid;
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task<string> GenerateBillNumberAsync(CancellationToken ct)
    {
        var year  = DateTime.UtcNow.Year;
        var count = await _db.Bills.Where(b => b.BillNumber.StartsWith($"INV-{year}-")).CountAsync(ct);
        return $"INV-{year}-{(count + 1):D5}";
    }

    private async Task<Bill> LoadBill(Guid billId, CancellationToken ct)
        => await _db.Bills
            .Include(b => b.Patient)
            .Include(b => b.CreatedBy)
            .Include(b => b.Items)
            .Include(b => b.Payments).ThenInclude(p => p.ReceivedBy)
            .Include(b => b.Adjustments).ThenInclude(a => a.AdjustedBy)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.BillId == billId, ct)
            ?? throw new NotFoundException("Bill not found.");

    private static BillResponse MapSummary(Bill b) => new()
    {
        BillId              = b.BillId,
        BillNumber          = b.BillNumber,
        PatientId           = b.PatientId,
        PatientName         = $"{b.Patient.FirstName} {b.Patient.LastName}",
        MedicalRecordNumber = b.Patient.MedicalRecordNumber,
        Status              = b.Status,
        TotalAmount         = b.TotalAmount,
        AdjustmentTotal     = b.AdjustmentTotal,
        DiscountAmount      = b.DiscountAmount,
        PaidAmount          = b.PaidAmount,
        BalanceDue          = b.BalanceDue,
        IssuedAt            = b.IssuedAt,
        CreatedAt           = b.CreatedAt,
    };

    private static BillDetailResponse MapDetail(Bill b) => new()
    {
        BillId              = b.BillId,
        BillNumber          = b.BillNumber,
        PatientId           = b.PatientId,
        PatientName         = $"{b.Patient.FirstName} {b.Patient.LastName}",
        MedicalRecordNumber = b.Patient.MedicalRecordNumber,
        Status              = b.Status,
        TotalAmount         = b.TotalAmount,
        AdjustmentTotal     = b.AdjustmentTotal,
        DiscountAmount      = b.DiscountAmount,
        PaidAmount          = b.PaidAmount,
        BalanceDue          = b.BalanceDue,
        IssuedAt            = b.IssuedAt,
        CreatedAt           = b.CreatedAt,
        DiscountReason      = b.DiscountReason,
        CreatedByName       = $"{b.CreatedBy.FirstName} {b.CreatedBy.LastName}",
        Notes               = b.Notes,
        UpdatedAt           = b.UpdatedAt,
        Items               = b.Items.Select(i => new BillItemResponse
        {
            ItemId      = i.ItemId,
            Description = i.Description,
            Category    = i.Category,
            Quantity    = i.Quantity,
            UnitPrice   = i.UnitPrice,
            TotalPrice  = i.TotalPrice,
            SourceType  = i.SourceType,
            SourceId    = i.SourceId,
        }).ToList(),
        Payments = b.Payments.OrderBy(p => p.PaymentDate).Select(p => new PaymentResponse
        {
            PaymentId      = p.PaymentId,
            Amount         = p.Amount,
            PaymentMethod  = p.PaymentMethod,
            Reference      = p.Reference,
            ReceivedByName = $"{p.ReceivedBy.FirstName} {p.ReceivedBy.LastName}",
            PaymentDate    = p.PaymentDate,
            Notes          = p.Notes,
            CreatedAt      = p.CreatedAt,
        }).ToList(),
        Adjustments = b.Adjustments.OrderBy(a => a.AdjustedAt).Select(a => new BillAdjustmentResponse
        {
            BillAdjustmentId = a.BillAdjustmentId,
            Amount           = a.Amount,
            Reason           = a.Reason,
            AdjustedByName   = $"{a.AdjustedBy.FirstName} {a.AdjustedBy.LastName}",
            AdjustedAt       = a.AdjustedAt,
        }).ToList(),
    };
}
