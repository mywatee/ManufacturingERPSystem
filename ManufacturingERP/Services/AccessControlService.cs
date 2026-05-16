using ManufacturingERP.Core;

namespace ManufacturingERP.Services;

public class AccessControlService : IAccessControlService
{
    private readonly IAuthService _authService;
    private readonly IPermissionService _permissionService;

    private int? _loadedForUserId;
    private Dictionary<string, RolePermissionDto>? _permissions;

    public AccessControlService(IAuthService authService, IPermissionService permissionService)
    {
        _authService = authService;
        _permissionService = permissionService;
    }

    public void Invalidate()
    {
        _loadedForUserId = null;
        _permissions = null;
    }

    public async Task RefreshAsync()
    {
        var user = _authService.CurrentUser;
        if (user == null)
        {
            Invalidate();
            return;
        }

        _permissions = await _permissionService.GetUserPermissionsAsync(user.UserId);
        _loadedForUserId = user.UserId;
    }

    public bool HasCached(string moduleKey, PermissionAction action)
    {
        var user = _authService.CurrentUser;
        if (user == null || _permissions == null || _loadedForUserId != user.UserId) return false;
        return HasFromDictionary(_permissions, moduleKey, action);
    }

    public async Task<bool> HasAsync(string moduleKey, PermissionAction action)
    {
        var user = _authService.CurrentUser;
        if (user == null) return false;

        if (_permissions == null || _loadedForUserId != user.UserId)
        {
            await RefreshAsync();
        }

        if (_permissions == null) return false;
        return HasFromDictionary(_permissions, moduleKey, action);
    }

    private static bool HasFromDictionary(Dictionary<string, RolePermissionDto> permissions, string moduleKey, PermissionAction action)
    {
        if (!permissions.TryGetValue(moduleKey, out var p)) return false;
        return action switch
        {
            PermissionAction.View => p.CanView,
            PermissionAction.Add => p.CanAdd,
            PermissionAction.Edit => p.CanEdit,
            PermissionAction.Delete => p.CanDelete,
            _ => false
        };
    }
}

