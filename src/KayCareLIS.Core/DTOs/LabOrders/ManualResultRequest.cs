namespace KayCareLIS.Core.DTOs.LabOrders;

public class ManualResultRequest
{
    public string  Result         { get; set; } = string.Empty;
    public string? Notes          { get; set; }
    public string? Unit           { get; set; }
    public string? ReferenceRange { get; set; }
}
