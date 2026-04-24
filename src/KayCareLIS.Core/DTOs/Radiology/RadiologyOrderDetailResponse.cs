namespace KayCareLIS.Core.DTOs.Radiology;

public class RadiologyOrderDetailResponse : RadiologyOrderResponse
{
    public IReadOnlyList<RadiologyOrderItemResponse> Items { get; set; } = [];
}
