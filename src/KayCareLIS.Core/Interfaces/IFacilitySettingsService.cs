using KayCareLIS.Core.DTOs.Facility;

namespace KayCareLIS.Core.Interfaces;

public interface IFacilitySettingsService
{
    Task<FacilitySettingsResponse?> GetAsync(CancellationToken ct = default);
    Task<FacilitySettingsResponse>  UpsertAsync(SaveFacilitySettingsRequest request, CancellationToken ct = default);
    Task<FacilitySettingsResponse>  UploadLogoAsync(Stream stream, string contentType, string extension, CancellationToken ct = default);
    Task<FacilitySettingsResponse>  DeleteLogoAsync(CancellationToken ct = default);
    Task<byte[]?>                   GetLogoBytesAsync(CancellationToken ct = default);
}
