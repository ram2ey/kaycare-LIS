using KayCareLIS.Core.Constants;
using KayCareLIS.Core.DTOs.Appointments;
using KayCareLIS.Core.Entities;
using KayCareLIS.Core.Exceptions;
using KayCareLIS.Core.Interfaces;
using KayCareLIS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KayCareLIS.Infrastructure.Services;

public class AppointmentService : IAppointmentService
{
    private readonly AppDbContext        _db;
    private readonly ICurrentUserService _currentUser;

    public AppointmentService(AppDbContext db, ICurrentUserService currentUser)
    {
        _db          = db;
        _currentUser = currentUser;
    }

    public async Task<AppointmentDetailResponse> CreateAsync(CreateAppointmentRequest request, CancellationToken ct = default)
    {
        var patient = await _db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.PatientId == request.PatientId, ct)
            ?? throw new NotFoundException("Patient not found.");
        var doctor = await _db.Users.AsNoTracking().Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == request.DoctorUserId, ct)
            ?? throw new NotFoundException("Doctor not found.");

        var appt = new Appointment
        {
            AppointmentId   = Guid.NewGuid(),
            PatientId       = request.PatientId,
            DoctorUserId    = request.DoctorUserId,
            ScheduledAt     = request.ScheduledAt,
            DurationMinutes = request.DurationMinutes > 0 ? request.DurationMinutes : 30,
            AppointmentType = request.AppointmentType,
            Status          = AppointmentStatus.Scheduled,
            ChiefComplaint  = request.ChiefComplaint?.Trim(),
            Room            = request.Room?.Trim(),
            Notes           = request.Notes?.Trim(),
            CreatedByUserId = _currentUser.UserId,
        };
        _db.Appointments.Add(appt);
        await _db.SaveChangesAsync(ct);
        appt.Patient = patient;
        appt.Doctor  = doctor;
        return MapDetail(appt);
    }

    public async Task<AppointmentDetailResponse> GetByIdAsync(Guid appointmentId, CancellationToken ct = default)
    {
        var appt = await Load(appointmentId, ct);
        return MapDetail(appt);
    }

    public async Task<AppointmentDetailResponse> UpdateAsync(Guid appointmentId, UpdateAppointmentRequest request, CancellationToken ct = default)
    {
        var appt = await Load(appointmentId, ct);

        if (appt.Status == AppointmentStatus.Cancelled || appt.Status == AppointmentStatus.Completed)
            throw new AppException("Cannot update a completed or cancelled appointment.");

        appt.ScheduledAt     = request.ScheduledAt ?? appt.ScheduledAt;
        appt.DurationMinutes = request.DurationMinutes is > 0 ? request.DurationMinutes.Value : appt.DurationMinutes;
        appt.AppointmentType = request.AppointmentType ?? appt.AppointmentType;
        appt.ChiefComplaint  = request.ChiefComplaint?.Trim();
        appt.Room            = request.Room?.Trim();
        appt.Notes           = request.Notes?.Trim();

        await _db.SaveChangesAsync(ct);
        return MapDetail(appt);
    }

    public async Task<AppointmentDetailResponse> TransitionStatusAsync(Guid appointmentId, string targetStatus, string? reason = null, CancellationToken ct = default)
    {
        var appt = await Load(appointmentId, ct);

        if (!AppointmentStatus.CanTransition(appt.Status, targetStatus))
            throw new ConflictException($"Cannot transition from '{appt.Status}' to '{targetStatus}'.");

        if (targetStatus == AppointmentStatus.Cancelled)
        {
            appt.CancelledAt         = DateTime.UtcNow;
            appt.CancelledByUserId   = _currentUser.UserId;
            appt.CancellationReason  = reason?.Trim();
        }
        appt.Status = targetStatus;
        await _db.SaveChangesAsync(ct);
        return MapDetail(appt);
    }

    public async Task<IReadOnlyList<AppointmentResponse>> GetCalendarAsync(CalendarRequest request, CancellationToken ct = default)
    {
        var from = request.From ?? DateTime.UtcNow.Date;
        var to   = request.To   ?? from.AddDays(1).AddSeconds(-1);

        var query = _db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .AsNoTracking()
            .Where(a => a.ScheduledAt >= from && a.ScheduledAt <= to);

        if (request.DoctorUserId.HasValue)
            query = query.Where(a => a.DoctorUserId == request.DoctorUserId.Value);

        var appts = await query.OrderBy(a => a.ScheduledAt).ToListAsync(ct);
        return appts.Select(MapSummary).ToList();
    }

    public async Task<IReadOnlyList<AppointmentResponse>> GetPatientAppointmentsAsync(Guid patientId, CancellationToken ct = default)
    {
        var appts = await _db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .AsNoTracking()
            .Where(a => a.PatientId == patientId)
            .OrderByDescending(a => a.ScheduledAt)
            .ToListAsync(ct);
        return appts.Select(MapSummary).ToList();
    }

    private async Task<Appointment> Load(Guid appointmentId, CancellationToken ct)
        => await _db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor).ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId, ct)
            ?? throw new NotFoundException("Appointment not found.");

    private static AppointmentResponse MapSummary(Appointment a) => new()
    {
        AppointmentId       = a.AppointmentId,
        PatientId           = a.PatientId,
        PatientName         = $"{a.Patient.FirstName} {a.Patient.LastName}",
        MedicalRecordNumber = a.Patient.MedicalRecordNumber,
        DoctorUserId        = a.DoctorUserId,
        DoctorName          = $"{a.Doctor.FirstName} {a.Doctor.LastName}",
        ScheduledAt         = a.ScheduledAt,
        DurationMinutes     = a.DurationMinutes,
        AppointmentType     = a.AppointmentType,
        Status              = a.Status,
        ChiefComplaint      = a.ChiefComplaint,
        Room                = a.Room,
    };

    private static AppointmentDetailResponse MapDetail(Appointment a) => new()
    {
        AppointmentId       = a.AppointmentId,
        PatientId           = a.PatientId,
        PatientName         = $"{a.Patient.FirstName} {a.Patient.LastName}",
        MedicalRecordNumber = a.Patient.MedicalRecordNumber,
        DoctorUserId        = a.DoctorUserId,
        DoctorName          = $"{a.Doctor.FirstName} {a.Doctor.LastName}",
        ScheduledAt         = a.ScheduledAt,
        DurationMinutes     = a.DurationMinutes,
        AppointmentType     = a.AppointmentType,
        Status              = a.Status,
        ChiefComplaint      = a.ChiefComplaint,
        Room                = a.Room,
        Notes               = a.Notes,
        CancelledAt         = a.CancelledAt,
        CancellationReason  = a.CancellationReason,
        CreatedAt           = a.CreatedAt,
        UpdatedAt           = a.UpdatedAt,
    };
}
