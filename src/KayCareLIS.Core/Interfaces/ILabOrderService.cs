using KayCareLIS.Core.DTOs.LabOrders;

namespace KayCareLIS.Core.Interfaces;

public interface ILabOrderService
{
    Task<IReadOnlyList<LabTestCatalogResponse>> GetTestCatalogAsync(CancellationToken ct);
    Task<LabOrderDetailResponse>               PlaceOrderAsync(CreateLabOrderRequest req, CancellationToken ct);
    Task<IReadOnlyList<LabOrderResponse>>      GetWaitingListAsync(DateOnly date, string? status, string? department, CancellationToken ct);
    Task<IReadOnlyList<LabOrderResponse>>      GetByPatientAsync(Guid patientId, CancellationToken ct);
    Task<LabOrderDetailResponse?>              GetByIdAsync(Guid labOrderId, CancellationToken ct);
    Task<LabOrderItemResponse> ReceiveSampleAsync(Guid labOrderItemId, CancellationToken ct);
    Task<LabOrderItemResponse> EnterManualResultAsync(Guid labOrderItemId, ManualResultRequest req, CancellationToken ct);
    Task<LabOrderItemResponse> SignItemAsync(Guid labOrderItemId, CancellationToken ct);
}
