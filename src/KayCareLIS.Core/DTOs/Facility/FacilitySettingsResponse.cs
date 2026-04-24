namespace KayCareLIS.Core.DTOs.Facility;

public class FacilitySettingsResponse
{
    public Guid    FacilitySettingsId { get; set; }
    public string  FacilityName       { get; set; } = string.Empty;
    public string? Address            { get; set; }
    public string? Phone              { get; set; }
    public string? Email              { get; set; }
    public string? LogoUrl            { get; set; }
    public bool    HasLogo            { get; set; }
    public DateTime UpdatedAt         { get; set; }
}
