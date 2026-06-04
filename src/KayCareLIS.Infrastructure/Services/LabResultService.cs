using KayCareLIS.Core.Constants;
using KayCareLIS.Core.DTOs.LabResults;
using KayCareLIS.Core.Entities;
using KayCareLIS.Core.Exceptions;
using KayCareLIS.Core.Interfaces;
using KayCareLIS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KayCareLIS.Infrastructure.Services;

public class LabResultService : ILabResultService
{
    private readonly AppDbContext _db;

    public LabResultService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<LabResultResponse>> GetByPatientAsync(Guid patientId, CancellationToken ct)
    {
        var results = await _db.LabResults
            .Include(r => r.Patient)
            .Include(r => r.OrderingDoctor)
            .Include(r => r.Observations)
            .AsNoTracking()
            .Where(r => r.PatientId == patientId)
            .OrderByDescending(r => r.ReceivedAt)
            .ToListAsync(ct);

        return results.Select(MapSummary).ToList();
    }

    public async Task<LabResultDetailResponse?> GetByAccessionAsync(string accessionNumber, CancellationToken ct)
    {
        var result = await _db.LabResults
            .Include(r => r.Patient)
            .Include(r => r.OrderingDoctor)
            .Include(r => r.Observations.OrderBy(o => o.SequenceNumber))
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.AccessionNumber == accessionNumber, ct);

        return result == null ? null : MapDetail(result);
    }

    public async Task<LabResultDetailResponse?> GetByIdAsync(Guid labResultId, CancellationToken ct)
    {
        var result = await _db.LabResults
            .Include(r => r.Patient)
            .Include(r => r.OrderingDoctor)
            .Include(r => r.Observations.OrderBy(o => o.SequenceNumber))
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.LabResultId == labResultId, ct);

        return result == null ? null : MapDetail(result);
    }

    private static LabResultResponse MapSummary(Core.Entities.LabResult r) => new()
    {
        LabResultId          = r.LabResultId,
        PatientId            = r.PatientId,
        PatientMrn           = r.Patient.MedicalRecordNumber,
        PatientName          = $"{r.Patient.FirstName} {r.Patient.LastName}",
        OrderingDoctorUserId = r.OrderingDoctorUserId,
        OrderingDoctorName   = r.OrderingDoctor == null ? null : $"{r.OrderingDoctor.FirstName} {r.OrderingDoctor.LastName}",
        AccessionNumber      = r.AccessionNumber,
        OrderCode            = r.OrderCode,
        OrderName            = r.OrderName,
        OrderedAt            = r.OrderedAt,
        ReceivedAt           = r.ReceivedAt,
        Status               = r.Status,
        ObservationCount     = r.Observations.Count,
        CreatedAt            = r.CreatedAt,
    };

    private static LabResultDetailResponse MapDetail(Core.Entities.LabResult r) => new()
    {
        LabResultId          = r.LabResultId,
        PatientId            = r.PatientId,
        PatientMrn           = r.Patient.MedicalRecordNumber,
        PatientName          = $"{r.Patient.FirstName} {r.Patient.LastName}",
        OrderingDoctorUserId = r.OrderingDoctorUserId,
        OrderingDoctorName   = r.OrderingDoctor == null ? null : $"{r.OrderingDoctor.FirstName} {r.OrderingDoctor.LastName}",
        AccessionNumber      = r.AccessionNumber,
        OrderCode            = r.OrderCode,
        OrderName            = r.OrderName,
        OrderedAt            = r.OrderedAt,
        ReceivedAt           = r.ReceivedAt,
        Status               = r.Status,
        ObservationCount     = r.Observations.Count,
        CreatedAt            = r.CreatedAt,
        Observations         = r.Observations.Select(o => new LabObservationResponse
        {
            LabObservationId = o.LabObservationId,
            SequenceNumber   = o.SequenceNumber,
            TestCode         = o.TestCode,
            TestName         = o.TestName,
            Value            = o.Value,
            Units            = o.Units,
            ReferenceRange   = o.ReferenceRange,
            AbnormalFlag     = o.AbnormalFlag,
        }).ToList(),
    };

    public async Task<bool> ProcessHl7MessageAsync(string rawMessage, CancellationToken ct)
    {
        var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.IsActive, ct);
        if (tenant == null) return false;

        var parsed = Hl7Parser.ParseOruR01(rawMessage, tenant.TenantId);
        if (parsed == null) return false;

        // Try to match to an open LabOrderItem by AccessionNumber
        var orderItem = await _db.LabOrderItems
            .FirstOrDefaultAsync(i => i.AccessionNumber == parsed.AccessionNumber
                && i.TenantId == tenant.TenantId, ct);

        Guid? patientId          = null;
        Guid? orderingDoctorId   = null;

        if (orderItem != null)
        {
            var order = await _db.LabOrders.AsNoTracking()
                .FirstOrDefaultAsync(o => o.LabOrderId == orderItem.LabOrderId, ct);
            if (order != null)
            {
                patientId        = order.PatientId;
                orderingDoctorId = order.OrderingDoctorUserId;
            }
        }

        // If we still have no patient, try MRN lookup from PID segment
        if (patientId == null && !string.IsNullOrEmpty(parsed.Hl7PatientId))
        {
            var patient = await _db.Patients.AsNoTracking()
                .FirstOrDefaultAsync(p => p.MedicalRecordNumber == parsed.Hl7PatientId
                    && p.TenantId == tenant.TenantId, ct);
            patientId = patient?.PatientId;
        }

        if (patientId == null) return false;

        // Check for duplicate accession
        var existing = await _db.LabResults
            .FirstOrDefaultAsync(r => r.AccessionNumber == parsed.AccessionNumber
                && r.TenantId == tenant.TenantId, ct);

        if (existing != null) return false;

        var result = new Core.Entities.LabResult
        {
            LabResultId          = Guid.NewGuid(),
            PatientId            = patientId.Value,
            OrderingDoctorUserId = orderingDoctorId,
            AccessionNumber      = parsed.AccessionNumber,
            OrderCode            = parsed.OrderCode,
            OrderName            = parsed.OrderName,
            OrderedAt            = parsed.OrderedAt,
            ReceivedAt           = DateTime.UtcNow,
            Status               = LabResultStatus.Received,
            RawHl7               = parsed.RawMessage,
            LabOrderItemId       = orderItem?.LabOrderItemId,
            TenantId             = tenant.TenantId,
        };
        _db.LabResults.Add(result);
        await _db.SaveChangesAsync(ct);

        var hasCritical = false;
        foreach (var obs in parsed.Observations)
        {
            obs.LabResultId = result.LabResultId;
            
            var catalogItem = await _db.LabTestCatalog
                .FirstOrDefaultAsync(t => t.TestCode == obs.TestCode, ct);
            if (catalogItem != null && !string.IsNullOrWhiteSpace(catalogItem.CriticalReferenceRange))
            {
                obs.IsCritical = LabOrderService.IsValueCritical(obs.Value, catalogItem.CriticalReferenceRange);
                if (obs.IsCritical)
                {
                    hasCritical = true;
                }
            }

            _db.LabObservations.Add(obs);
        }
        await _db.SaveChangesAsync(ct);

        // Update linked LabOrderItem status
        if (orderItem != null)
        {
            orderItem.LabResultId = result.LabResultId;
            orderItem.Status      = LabOrderItemStatus.Resulted;
            orderItem.ResultedAt  = DateTime.UtcNow;
            orderItem.IsCritical  = hasCritical;
            await _db.SaveChangesAsync(ct);
        }

        return true;
    }
}
