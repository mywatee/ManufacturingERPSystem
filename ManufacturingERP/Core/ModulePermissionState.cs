using ManufacturingERP.Services;

namespace ManufacturingERP.Core;

public readonly record struct ModulePermissionState(bool CanAdd, bool CanEdit, bool CanDelete);

public static class ModulePermissionStateFactory
{
    public static ModulePermissionState FromAccessControl(IAccessControlService accessControl, string moduleKey)
    {
        return new ModulePermissionState(
            accessControl.HasCached(moduleKey, PermissionAction.Add),
            accessControl.HasCached(moduleKey, PermissionAction.Edit),
            accessControl.HasCached(moduleKey, PermissionAction.Delete));
    }
}
