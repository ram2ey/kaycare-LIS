namespace KayCareLIS.Core.Entities;

public class LabTestCatalog
{
    public Guid   LabTestCatalogId      { get; set; }
    public string TestCode              { get; set; } = string.Empty;
    public string TestName              { get; set; } = string.Empty;
    public string Department            { get; set; } = string.Empty;
    public string? InstrumentType       { get; set; }
    public bool    IsManualEntry        { get; set; }
    public int     TatHours             { get; set; } = 4;
    public bool    IsActive             { get; set; } = true;
    public string? DefaultUnit          { get; set; }
    public string? DefaultReferenceRange { get; set; }
}
