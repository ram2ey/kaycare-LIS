namespace KayCareLIS.Core.Entities;

public class LabOrderItem
{
    public Guid   LabOrderItemId   { get; set; }
    public Guid   LabOrderId       { get; set; }
    public Guid   TenantId         { get; set; }
    public Guid   LabTestCatalogId { get; set; }

    public string  TestName         { get; set; } = string.Empty;
    public string  Department       { get; set; } = string.Empty;
    public string? InstrumentType   { get; set; }
    public bool    IsManualEntry    { get; set; }
    public int     TatHours         { get; set; }
    public string? AccessionNumber  { get; set; }

    public string    Status           { get; set; } = "Ordered";
    public DateTime? SampleReceivedAt { get; set; }
    public DateTime? ResultedAt       { get; set; }
    public DateTime? SignedAt         { get; set; }
    public Guid?     SignedByUserId   { get; set; }

    public string? ManualResult               { get; set; }
    public string? ManualResultNotes          { get; set; }
    public string? ManualResultUnit           { get; set; }
    public string? ManualResultReferenceRange { get; set; }
    public string? ManualResultFlag           { get; set; }

    public Guid? LabResultId { get; set; }

    public LabOrder       LabOrder        { get; set; } = null!;
    public LabTestCatalog LabTestCatalog  { get; set; } = null!;
    public LabResult?     LabResult       { get; set; }
}
