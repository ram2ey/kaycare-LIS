using KayCareLIS.Core.DTOs.Common;
using KayCareLIS.Core.DTOs.Patients;

namespace KayCareLIS.Core.Interfaces;

public interface IPatientService
{
    Task<PatientDetailResponse> RegisterAsync(CreatePatientRequest request, CancellationToken ct = default);
    Task<PagedResult<PatientResponse>> SearchAsync(PatientSearchRequest request, CancellationToken ct = default);
    Task<PatientDetailResponse> GetByIdAsync(Guid patientId, CancellationToken ct = default);
    Task<PatientDetailResponse> UpdateAsync(Guid patientId, UpdatePatientRequest request, CancellationToken ct = default);
}
