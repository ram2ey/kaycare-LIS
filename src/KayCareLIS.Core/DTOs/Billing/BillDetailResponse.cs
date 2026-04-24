namespace KayCareLIS.Core.DTOs.Billing;

public class BillDetailResponse : BillResponse
{
    public string?   DiscountReason { get; set; }
    public string    CreatedByName  { get; set; } = string.Empty;
    public string?   Notes         { get; set; }
    public DateTime  UpdatedAt     { get; set; }
    public List<BillItemResponse>       Items       { get; set; } = [];
    public List<PaymentResponse>        Payments    { get; set; } = [];
    public List<BillAdjustmentResponse> Adjustments { get; set; } = [];
}
