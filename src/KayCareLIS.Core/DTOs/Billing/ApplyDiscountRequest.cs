using System.ComponentModel.DataAnnotations;

namespace KayCareLIS.Core.DTOs.Billing;

public class ApplyDiscountRequest
{
    [Range(0, double.MaxValue)]
    public decimal DiscountAmount { get; set; }

    [MaxLength(500)]
    public string? DiscountReason { get; set; }
}
