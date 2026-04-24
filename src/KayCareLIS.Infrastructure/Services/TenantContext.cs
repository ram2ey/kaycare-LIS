using KayCareLIS.Core.Interfaces;

namespace KayCareLIS.Infrastructure.Services;

public class TenantContext : ITenantContext
{
    public Guid TenantId { get; set; }
    public string TenantCode { get; set; } = string.Empty;
}
