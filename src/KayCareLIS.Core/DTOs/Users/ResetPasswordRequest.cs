using System.ComponentModel.DataAnnotations;

namespace KayCareLIS.Core.DTOs.Users;

public class ResetPasswordRequest
{
    [Required, MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;
}
