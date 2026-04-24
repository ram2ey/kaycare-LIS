namespace KayCareLIS.Core.DTOs.LabOrders;

public class LabOrderResponse
{
    public Guid    LabOrderId           { get; set; }
    public Guid    PatientId            { get; set; }
    public string  PatientName          { get; set; } = string.Empty;
    public string  PatientMrn           { get; set; } = string.Empty;
    public string  PatientGender        { get; set; } = string.Empty;
    public DateOnly PatientDob          { get; set; }
    public Guid?   BillId               { get; set; }
    public string? BillNumber           { get; set; }
    public Guid    OrderingDoctorUserId { get; set; }
    public string  OrderingDoctorName   { get; set; } = string.Empty;
    public string  Organisation         { get; set; } = string.Empty;
    public string  Status               { get; set; } = string.Empty;
    public string? Notes                { get; set; }
    public DateTime OrderedAt           { get; set; }
    public int IncompleteCount { get; set; }
    public int CompletedCount  { get; set; }
    public int SignedCount     { get; set; }
    public IReadOnlyList<string> TestNames { get; set; } = [];
}
