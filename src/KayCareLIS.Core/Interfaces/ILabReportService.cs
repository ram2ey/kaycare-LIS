namespace KayCareLIS.Core.Interfaces;

public interface ILabReportService
{
    Task<byte[]?> GenerateLabOrderReportAsync(Guid labOrderId, CancellationToken ct);
}
