namespace KayCareLIS.Core.DTOs.LabOrders;

public class UpdateLabTestCatalogRequest
{
    public string TestCode { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string? InstrumentType { get; set; }
    public bool IsManualEntry { get; set; }
    public int TatHours { get; set; }
    public string? DefaultUnit { get; set; }
    public string? DefaultReferenceRange { get; set; }
    public string? CriticalReferenceRange { get; set; }
    public bool IsActive { get; set; }
}
