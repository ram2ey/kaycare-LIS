using KayCareLIS.Core.Constants;
using KayCareLIS.Core.DTOs.LabOrders;
using KayCareLIS.Core.Entities;
using KayCareLIS.Core.Exceptions;
using KayCareLIS.Core.Interfaces;
using KayCareLIS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KayCareLIS.Infrastructure.Services;

public class LabOrderService : ILabOrderService
{
    private readonly AppDbContext        _db;
    private readonly ITenantContext      _tenantContext;
    private readonly ICurrentUserService _currentUser;

    public LabOrderService(AppDbContext db, ITenantContext tenantContext, ICurrentUserService currentUser)
    {
        _db            = db;
        _tenantContext = tenantContext;
        _currentUser   = currentUser;
    }

    public async Task<IReadOnlyList<LabTestCatalogResponse>> GetTestCatalogAsync(CancellationToken ct)
    {
        var tests = await _db.LabTestCatalog
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Department)
            .ThenBy(t => t.TestName)
            .ToListAsync(ct);

        return tests.Select(t => new LabTestCatalogResponse
        {
            LabTestCatalogId      = t.LabTestCatalogId,
            TestCode              = t.TestCode,
            TestName              = t.TestName,
            Department            = t.Department,
            InstrumentType        = t.InstrumentType,
            IsManualEntry         = t.IsManualEntry,
            TatHours              = t.TatHours,
            DefaultUnit           = t.DefaultUnit,
            DefaultReferenceRange = t.DefaultReferenceRange,
        }).ToList();
    }

    public async Task<LabOrderDetailResponse> PlaceOrderAsync(CreateLabOrderRequest req, CancellationToken ct)
    {
        if (req.TestIds.Count == 0) throw new AppException("At least one test must be selected.");

        var patient = await _db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.PatientId == req.PatientId, ct)
            ?? throw new NotFoundException("Patient not found.");

        var catalogItems = await _db.LabTestCatalog
            .Where(t => req.TestIds.Contains(t.LabTestCatalogId) && t.IsActive)
            .ToListAsync(ct);

        if (catalogItems.Count != req.TestIds.Count)
            throw new AppException("One or more selected tests were not found or are inactive.");

        var doctor = await _db.Users.AsNoTracking().Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == _currentUser.UserId, ct)
            ?? throw new NotFoundException("Ordering doctor not found.");

        var order = new LabOrder
        {
            LabOrderId           = Guid.NewGuid(),
            PatientId            = req.PatientId,
            BillId               = req.BillId,
            OrderingDoctorUserId = _currentUser.UserId,
            Organisation         = req.Organisation ?? "DIRECT",
            Status               = LabOrderStatus.Pending,
            Notes                = req.Notes?.Trim(),
        };
        _db.LabOrders.Add(order);
        await _db.SaveChangesAsync(ct);

        foreach (var test in catalogItems)
        {
            var accession = await GenerateAccessionAsync(ct);
            var item = new LabOrderItem
            {
                LabOrderItemId   = Guid.NewGuid(),
                LabOrderId       = order.LabOrderId,
                TenantId         = _tenantContext.TenantId,
                LabTestCatalogId = test.LabTestCatalogId,
                TestName         = test.TestName,
                Department       = test.Department,
                InstrumentType   = test.InstrumentType,
                IsManualEntry    = test.IsManualEntry,
                TatHours         = test.TatHours,
                AccessionNumber  = accession,
                Status           = LabOrderItemStatus.Ordered,
            };
            _db.LabOrderItems.Add(item);
        }

        order.Status = LabOrderStatus.Active;
        await _db.SaveChangesAsync(ct);

        return await LoadDetailAsync(order.LabOrderId, ct);
    }

    public async Task<IReadOnlyList<LabOrderResponse>> GetWaitingListAsync(DateOnly date, string? status, string? department, CancellationToken ct)
    {
        var from = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var to   = date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var query = _db.LabOrders
            .Include(o => o.Patient)
            .Include(o => o.OrderingDoctor)
            .Include(o => o.Items)
            .AsNoTracking()
            .Where(o => o.CreatedAt >= from && o.CreatedAt <= to);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(o => o.Status == status);

        if (!string.IsNullOrEmpty(department))
            query = query.Where(o => o.Items.Any(i => i.Department == department));

        var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync(ct);
        return orders.Select(MapSummary).ToList();
    }

    public async Task<IReadOnlyList<LabOrderResponse>> GetByPatientAsync(Guid patientId, CancellationToken ct)
    {
        var orders = await _db.LabOrders
            .Include(o => o.Patient)
            .Include(o => o.OrderingDoctor)
            .Include(o => o.Items)
            .AsNoTracking()
            .Where(o => o.PatientId == patientId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);
        return orders.Select(MapSummary).ToList();
    }

    public async Task<LabOrderDetailResponse?> GetByIdAsync(Guid labOrderId, CancellationToken ct)
    {
        var order = await _db.LabOrders
            .Include(o => o.Patient)
            .Include(o => o.OrderingDoctor)
            .Include(o => o.Bill)
            .Include(o => o.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.LabOrderId == labOrderId, ct);
        if (order == null) return null;

        var response = MapSummary(order);
        return new LabOrderDetailResponse
        {
            LabOrderId           = response.LabOrderId,
            PatientId            = response.PatientId,
            PatientName          = response.PatientName,
            PatientMrn           = response.PatientMrn,
            PatientGender        = response.PatientGender,
            PatientDob           = response.PatientDob,
            BillId               = response.BillId,
            BillNumber           = response.BillNumber,
            OrderingDoctorUserId = response.OrderingDoctorUserId,
            OrderingDoctorName   = response.OrderingDoctorName,
            Organisation         = response.Organisation,
            Status               = response.Status,
            Notes                = response.Notes,
            OrderedAt            = response.OrderedAt,
            IncompleteCount      = response.IncompleteCount,
            CompletedCount       = response.CompletedCount,
            SignedCount          = response.SignedCount,
            TestNames            = response.TestNames,
            Items                = order.Items.Select(MapItem).ToList(),
        };
    }

    public async Task<LabOrderItemResponse> ReceiveSampleAsync(Guid labOrderItemId, CancellationToken ct)
    {
        var item = await LoadItem(labOrderItemId, ct);

        if (item.Status != LabOrderItemStatus.Ordered)
            throw new ConflictException("Sample can only be received on items in Ordered status.");

        item.Status           = LabOrderItemStatus.SampleReceived;
        item.SampleReceivedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        await UpdateOrderStatusAsync(item.LabOrderId, ct);
        return MapItem(item);
    }

    public async Task<LabOrderItemResponse> EnterManualResultAsync(Guid labOrderItemId, ManualResultRequest req, CancellationToken ct)
    {
        var item = await LoadItem(labOrderItemId, ct);

        if (item.Status == LabOrderItemStatus.Signed)
            throw new ConflictException("Cannot update a signed result.");

        item.ManualResult               = req.Result?.Trim();
        item.ManualResultNotes          = req.Notes?.Trim();
        item.ManualResultUnit           = req.Unit?.Trim();
        item.ManualResultReferenceRange = req.ReferenceRange?.Trim();
        item.ManualResultFlag           = ComputeFlag(req.Result, req.ReferenceRange ?? item.ManualResultReferenceRange);
        item.Status                     = LabOrderItemStatus.Resulted;
        item.ResultedAt                 = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        await UpdateOrderStatusAsync(item.LabOrderId, ct);
        return MapItem(item);
    }

    public async Task<LabOrderItemResponse> SignItemAsync(Guid labOrderItemId, CancellationToken ct)
    {
        var item = await LoadItem(labOrderItemId, ct);

        if (item.Status != LabOrderItemStatus.Resulted)
            throw new ConflictException("Only resulted items can be signed.");

        item.Status          = LabOrderItemStatus.Signed;
        item.SignedAt        = DateTime.UtcNow;
        item.SignedByUserId  = _currentUser.UserId;

        await _db.SaveChangesAsync(ct);
        await UpdateOrderStatusAsync(item.LabOrderId, ct);
        return MapItem(item);
    }

    private async Task UpdateOrderStatusAsync(Guid labOrderId, CancellationToken ct)
    {
        var items = await _db.LabOrderItems.Where(i => i.LabOrderId == labOrderId).ToListAsync(ct);
        var order = await _db.LabOrders.FirstOrDefaultAsync(o => o.LabOrderId == labOrderId, ct);
        if (order == null) return;

        if (items.All(i => i.Status == LabOrderItemStatus.Signed))
            order.Status = LabOrderStatus.Signed;
        else if (items.All(i => i.Status == LabOrderItemStatus.Resulted || i.Status == LabOrderItemStatus.Signed))
            order.Status = LabOrderStatus.Completed;
        else if (items.Any(i => i.Status == LabOrderItemStatus.Resulted || i.Status == LabOrderItemStatus.Signed))
            order.Status = LabOrderStatus.PartiallyCompleted;
        else
            order.Status = LabOrderStatus.Active;

        await _db.SaveChangesAsync(ct);
    }

    private async Task<LabOrderDetailResponse> LoadDetailAsync(Guid labOrderId, CancellationToken ct)
    {
        var result = await GetByIdAsync(labOrderId, ct);
        return result ?? throw new NotFoundException("Lab order not found.");
    }

    private async Task<LabOrderItem> LoadItem(Guid labOrderItemId, CancellationToken ct)
        => await _db.LabOrderItems.FirstOrDefaultAsync(i => i.LabOrderItemId == labOrderItemId, ct)
            ?? throw new NotFoundException("Lab order item not found.");

    private async Task<string> GenerateAccessionAsync(CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var count = await _db.LabOrderItems
            .Where(i => i.AccessionNumber != null && i.AccessionNumber.StartsWith($"ACC-{year}-"))
            .CountAsync(ct);
        return $"ACC-{year}-{(count + 1):D5}";
    }

    internal static string? ComputeFlag(string? value, string? rangeStr)
    {
        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(rangeStr))
            return null;

        if (!double.TryParse(value, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var numericValue))
            return null;

        var parts = rangeStr.Split('-');
        if (parts.Length != 2) return null;

        if (!double.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var low)) return null;
        if (!double.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var high)) return null;

        if (numericValue < low)  return "L";
        if (numericValue > high) return "H";
        return "N";
    }

    private static LabOrderResponse MapSummary(LabOrder o)
    {
        var incomplete = o.Items.Count(i => i.Status == LabOrderItemStatus.Ordered || i.Status == LabOrderItemStatus.SampleReceived);
        var completed  = o.Items.Count(i => i.Status == LabOrderItemStatus.Resulted);
        var signed     = o.Items.Count(i => i.Status == LabOrderItemStatus.Signed);

        return new LabOrderResponse
        {
            LabOrderId           = o.LabOrderId,
            PatientId            = o.PatientId,
            PatientName          = $"{o.Patient.FirstName} {o.Patient.LastName}",
            PatientMrn           = o.Patient.MedicalRecordNumber,
            PatientGender        = o.Patient.Gender,
            PatientDob           = o.Patient.DateOfBirth,
            BillId               = o.BillId,
            BillNumber           = o.Bill?.BillNumber,
            OrderingDoctorUserId = o.OrderingDoctorUserId,
            OrderingDoctorName   = $"{o.OrderingDoctor.FirstName} {o.OrderingDoctor.LastName}",
            Organisation         = o.Organisation,
            Status               = o.Status,
            Notes                = o.Notes,
            OrderedAt            = o.CreatedAt,
            IncompleteCount      = incomplete,
            CompletedCount       = completed,
            SignedCount          = signed,
            TestNames            = o.Items.Select(i => i.TestName).ToList(),
        };
    }

    private static LabOrderItemResponse MapItem(LabOrderItem i) => new()
    {
        LabOrderItemId             = i.LabOrderItemId,
        LabTestCatalogId           = i.LabTestCatalogId,
        TestName                   = i.TestName,
        Department                 = i.Department,
        InstrumentType             = i.InstrumentType,
        IsManualEntry              = i.IsManualEntry,
        TatHours                   = i.TatHours,
        AccessionNumber            = i.AccessionNumber,
        Status                     = i.Status,
        SampleReceivedAt           = i.SampleReceivedAt,
        ResultedAt                 = i.ResultedAt,
        SignedAt                   = i.SignedAt,
        ManualResult               = i.ManualResult,
        ManualResultNotes          = i.ManualResultNotes,
        ManualResultUnit           = i.ManualResultUnit,
        ManualResultReferenceRange = i.ManualResultReferenceRange,
        ManualResultFlag           = i.ManualResultFlag,
        LabResultId                = i.LabResultId,
        IsTatExceeded              = i.SampleReceivedAt.HasValue
            && i.Status != LabOrderItemStatus.Signed
            && DateTime.UtcNow > i.SampleReceivedAt.Value.AddHours(i.TatHours),
    };
}
