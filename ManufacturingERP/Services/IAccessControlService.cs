using ManufacturingERP.Core;

namespace ManufacturingERP.Services;

public interface IAccessControlService
{
    Task RefreshAsync();
    void Invalidate();
    bool HasCached(string moduleKey, PermissionAction action);
    Task<bool> HasAsync(string moduleKey, PermissionAction action);
}

