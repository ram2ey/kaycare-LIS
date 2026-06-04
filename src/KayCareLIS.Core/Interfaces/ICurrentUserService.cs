namespace KayCareLIS.Core.Interfaces;

public interface ICurrentUserService
{
    Guid   UserId        { get; }
    Guid   TenantId      { get; }
    string Email         { get; }
    string Role          { get; }
    string? Department   { get; }
    bool   IsAuthenticated { get; }
}
