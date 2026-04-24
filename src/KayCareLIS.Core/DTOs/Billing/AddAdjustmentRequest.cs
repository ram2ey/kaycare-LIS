using System.ComponentModel.DataAnnotations;

namespace KayCareLIS.Core.DTOs.Billing;

public class AddAdjustmentRequest
{
    [Range(-1_000_000, 1_000_000)]
    public decimal Amount { get; set; }

    [Required, MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}
