namespace KayCareLIS.Core.Entities;

public class LabResult : TenantEntity
{
    public Guid   LabResultId          { get; set; }
    public Guid   PatientId            { get; set; }
    public Guid?  OrderingDoctorUserId { get; set; }

    public string  AccessionNumber { get; set; } = string.Empty;
    public string? OrderCode       { get; set; }
    public string? OrderName       { get; set; }
    public DateTime? OrderedAt     { get; set; }
    public DateTime  ReceivedAt    { get; set; }

    public string  Status  { get; set; } = "Received";
    public string? RawHl7  { get; set; }

    public Guid? LabOrderItemId { get; set; }

    public Patient             Patient        { get; set; } = null!;
    public User?               OrderingDoctor { get; set; }
    public LabOrderItem?       LabOrderItem   { get; set; }
    public ICollection<LabObservation> Observations { get; set; } = [];
}
