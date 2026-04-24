namespace KayCareLIS.Core.DTOs.LabOrders;

public class CreateLabOrderRequest
{
    public Guid   PatientId  { get; set; }
    public Guid?  BillId     { get; set; }
    public string Organisation { get; set; } = "DIRECT";
    public string? Notes     { get; set; }
    public List<Guid> TestIds { get; set; } = [];
}
