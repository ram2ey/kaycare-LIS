namespace KayCareLIS.Core.Entities;

public class FacilitySettings : TenantEntity
{
    public Guid    FacilitySettingsId { get; set; }
    public string  FacilityName       { get; set; } = string.Empty;
    public string? Address            { get; set; }
    public string? Phone              { get; set; }
    public string? Email              { get; set; }
    public string? LogoBlobName       { get; set; }
}
