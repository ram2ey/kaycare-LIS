namespace KayCareLIS.Core.Interfaces;

public interface IBillingPdfService
{
    Task<byte[]?> GenerateBillInvoiceAsync(Guid billId, CancellationToken ct);
    Task<byte[]?> GeneratePaymentReceiptAsync(Guid paymentId, CancellationToken ct);
}
