using KayCareLIS.Core.Entities;

namespace KayCareLIS.Core.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user, string roleName);
}
