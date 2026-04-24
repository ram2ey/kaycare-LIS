namespace KayCareLIS.Core.DTOs.LabOrders;

public class LabOrderDetailResponse : LabOrderResponse
{
    public IReadOnlyList<LabOrderItemResponse> Items { get; set; } = [];
}
