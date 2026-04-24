using KayCareLIS.Core.DTOs.Common;
using KayCareLIS.Core.DTOs.Patients;
using KayCareLIS.Core.Entities;
using KayCareLIS.Core.Exceptions;
using KayCareLIS.Core.Interfaces;
using KayCareLIS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KayCareLIS.Infrastructure.Services;

public class PatientService : IPatientService
{
    private readonly AppDbContext        _db;
    private readonly ITenantContext      _tenantContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService       _audit;

    public PatientService(AppDbContext db, ITenantContext tenantContext, ICurrentUserService currentUser, IAuditService audit)
    {
        _db            = db;
        _tenantContext = tenantContext;
        _currentUser   = currentUser;
        _audit         = audit;
    }

    public async Task<PatientDetailResponse> RegisterAsync(CreatePatientRequest request, CancellationToken ct = default)
    {
        var mrn = await GenerateMrnAsync(ct);
        var patient = new Patient
        {
            PatientId                  = Guid.NewGuid(),
            MedicalRecordNumber        = mrn,
            FirstName                  = request.FirstName.Trim(),
            MiddleName                 = request.MiddleName?.Trim(),
            LastName                   = request.LastName.Trim(),
            DateOfBirth                = request.DateOfBirth,
            Gender                     = request.Gender,
            BloodType                  = request.BloodType,
            NationalId                 = request.NationalId?.Trim(),
            Email                      = request.Email?.ToLower().Trim(),
            PhoneNumber                = request.PhoneNumber?.Trim(),
            AlternatePhone             = request.AlternatePhone?.Trim(),
            AddressLine1               = request.AddressLine1?.Trim(),
            AddressLine2               = request.AddressLine2?.Trim(),
            City                       = request.City?.Trim(),
            State                      = request.State?.Trim(),
            PostalCode                 = request.PostalCode?.Trim(),
            Country                    = request.Country?.Trim() ?? "GH",
            EmergencyContactName       = request.EmergencyContactName?.Trim(),
            EmergencyContactPhone      = request.EmergencyContactPhone?.Trim(),
            EmergencyContactRelation   = request.EmergencyContactRelation?.Trim(),
            InsuranceProvider          = request.InsuranceProvider?.Trim(),
            InsurancePolicyNumber      = request.InsurancePolicyNumber?.Trim(),
            InsuranceGroupNumber       = request.InsuranceGroupNumber?.Trim(),
            RegisteredByUserId         = _currentUser.UserId,
            IsActive                   = true,
        };
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("Patient.Create", "Patient", patient.PatientId, patient.PatientId, null, ct);
        return MapDetail(patient);
    }

    public async Task<PagedResult<PatientResponse>> SearchAsync(PatientSearchRequest request, CancellationToken ct = default)
    {
        var query = _db.Patients.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var s = request.Query.ToLower();
            query = query.Where(p =>
                p.FirstName.ToLower().Contains(s) ||
                p.LastName.ToLower().Contains(s) ||
                p.MedicalRecordNumber.ToLower().Contains(s) ||
                (p.PhoneNumber != null && p.PhoneNumber.Contains(s)));
        }

        var total = await query.CountAsync(ct);
        var page  = request.Page < 1 ? 1 : request.Page;
        var size  = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 100);

        var items = await query
            .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        return new PagedResult<PatientResponse>
        {
            Items      = items.Select(MapSummary).ToList(),
            TotalCount = total,
            Page       = page,
            PageSize   = size,
        };
    }

    public async Task<PatientDetailResponse> GetByIdAsync(Guid patientId, CancellationToken ct = default)
    {
        var patient = await _db.Patients.AsNoTracking()
            .FirstOrDefaultAsync(p => p.PatientId == patientId, ct)
            ?? throw new NotFoundException("Patient not found.");
        await _audit.LogAsync("Patient.View", "Patient", patientId, patientId, null, ct);
        return MapDetail(patient);
    }

    public async Task<PatientDetailResponse> UpdateAsync(Guid patientId, UpdatePatientRequest request, CancellationToken ct = default)
    {
        var patient = await _db.Patients.FirstOrDefaultAsync(p => p.PatientId == patientId, ct)
            ?? throw new NotFoundException("Patient not found.");

        patient.FirstName                = request.FirstName?.Trim() ?? patient.FirstName;
        patient.MiddleName               = request.MiddleName?.Trim();
        patient.LastName                 = request.LastName?.Trim() ?? patient.LastName;
        patient.BloodType                = request.BloodType;
        patient.NationalId               = request.NationalId?.Trim();
        patient.Email                    = request.Email?.ToLower().Trim();
        patient.PhoneNumber              = request.PhoneNumber?.Trim();
        patient.AlternatePhone           = request.AlternatePhone?.Trim();
        patient.AddressLine1             = request.AddressLine1?.Trim();
        patient.AddressLine2             = request.AddressLine2?.Trim();
        patient.City                     = request.City?.Trim();
        patient.State                    = request.State?.Trim();
        patient.PostalCode               = request.PostalCode?.Trim();
        patient.Country                  = request.Country?.Trim() ?? patient.Country;
        patient.EmergencyContactName     = request.EmergencyContactName?.Trim();
        patient.EmergencyContactPhone    = request.EmergencyContactPhone?.Trim();
        patient.EmergencyContactRelation = request.EmergencyContactRelation?.Trim();
        patient.InsuranceProvider        = request.InsuranceProvider?.Trim();
        patient.InsurancePolicyNumber    = request.InsurancePolicyNumber?.Trim();
        patient.InsuranceGroupNumber     = request.InsuranceGroupNumber?.Trim();

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("Patient.Update", "Patient", patientId, patientId, null, ct);
        return MapDetail(patient);
    }

    private async Task<string> GenerateMrnAsync(CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var count = await _db.Patients
            .Where(p => p.MedicalRecordNumber.StartsWith($"MRN-{year}-"))
            .CountAsync(ct);
        return $"MRN-{year}-{(count + 1):D5}";
    }

    private static int CalcAge(DateOnly dob)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - dob.Year;
        if (dob.AddYears(age) > today) age--;
        return age;
    }

    private static PatientResponse MapSummary(Patient p) => new()
    {
        PatientId           = p.PatientId,
        MedicalRecordNumber = p.MedicalRecordNumber,
        FullName            = $"{p.FirstName} {p.LastName}",
        DateOfBirth         = p.DateOfBirth,
        Age                 = CalcAge(p.DateOfBirth),
        Gender              = p.Gender,
        PhoneNumber         = p.PhoneNumber,
        InsuranceProvider   = p.InsuranceProvider,
        IsActive            = p.IsActive,
        RegisteredAt        = p.CreatedAt,
    };

    private static PatientDetailResponse MapDetail(Patient p) => new()
    {
        PatientId                  = p.PatientId,
        MedicalRecordNumber        = p.MedicalRecordNumber,
        FullName                   = $"{p.FirstName} {p.LastName}",
        DateOfBirth                = p.DateOfBirth,
        Age                        = CalcAge(p.DateOfBirth),
        Gender                     = p.Gender,
        PhoneNumber                = p.PhoneNumber,
        InsuranceProvider          = p.InsuranceProvider,
        IsActive                   = p.IsActive,
        RegisteredAt               = p.CreatedAt,
        MiddleName                 = p.MiddleName,
        BloodType                  = p.BloodType,
        NationalId                 = p.NationalId,
        Email                      = p.Email,
        AlternatePhone             = p.AlternatePhone,
        AddressLine1               = p.AddressLine1,
        AddressLine2               = p.AddressLine2,
        City                       = p.City,
        State                      = p.State,
        PostalCode                 = p.PostalCode,
        Country                    = p.Country,
        EmergencyContactName       = p.EmergencyContactName,
        EmergencyContactPhone      = p.EmergencyContactPhone,
        EmergencyContactRelation   = p.EmergencyContactRelation,
        InsurancePolicyNumber      = p.InsurancePolicyNumber,
        InsuranceGroupNumber       = p.InsuranceGroupNumber,
    };
}
