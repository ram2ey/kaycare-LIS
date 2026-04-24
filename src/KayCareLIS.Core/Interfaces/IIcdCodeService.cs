using KayCareLIS.Core.DTOs.IcdCodes;

namespace KayCareLIS.Core.Interfaces;

public interface IIcdCodeService
{
    Task<IReadOnlyList<IcdCodeResponse>> SearchAsync(string query, int limit, CancellationToken ct);
}
