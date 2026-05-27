namespace ManufacturingERP.Core;

public readonly record struct ModulePermissionFlags(bool CanView, bool CanAdd, bool CanEdit, bool CanDelete);

public static class RolePermissionDefaults
{
    public const string DefaultPassword = "NhanVien@123";
    public const string PermissionsVersion = "1";

    private static readonly ModulePermissionFlags None = new(false, false, false, false);
    private static readonly ModulePermissionFlags View = new(true, false, false, false);
    private static readonly ModulePermissionFlags ViewEdit = new(true, false, true, false);
    private static readonly ModulePermissionFlags Crud = new(true, true, true, true);

    private static readonly Dictionary<string, Dictionary<string, ModulePermissionFlags>> Matrix = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Admin"] = Build(
            (SystemModules.Dashboard, Crud),
            (SystemModules.SystemAdmin, Crud),
            (SystemModules.MasterData, Crud),
            (SystemModules.Production, Crud),
            (SystemModules.QualityControl, Crud),
            (SystemModules.Warehouse, Crud),
            (SystemModules.HumanResources, Crud),
            (SystemModules.Finance, Crud)),

        ["Quản lý sản xuất"] = Build(
            (SystemModules.Dashboard, View),
            (SystemModules.MasterData, ViewEdit),
            (SystemModules.Production, Crud),
            (SystemModules.QualityControl, View),
            (SystemModules.Warehouse, View),
            (SystemModules.HumanResources, View),
            (SystemModules.Finance, View)),

        ["Nhân viên vận hành"] = Build(
            (SystemModules.Dashboard, View),
            (SystemModules.MasterData, View),
            (SystemModules.Production, ViewEdit),
            (SystemModules.Warehouse, View)),

        ["Quản lý kho"] = Build(
            (SystemModules.Dashboard, View),
            (SystemModules.MasterData, View),
            (SystemModules.Production, View),
            (SystemModules.QualityControl, View),
            (SystemModules.Warehouse, Crud),
            (SystemModules.Finance, View)),

        ["QC"] = Build(
            (SystemModules.Dashboard, View),
            (SystemModules.MasterData, View),
            (SystemModules.Production, View),
            (SystemModules.QualityControl, Crud),
            (SystemModules.Warehouse, View)),

        ["Kế toán"] = Build(
            (SystemModules.Dashboard, View),
            (SystemModules.MasterData, View),
            (SystemModules.Warehouse, View),
            (SystemModules.HumanResources, ViewEdit),
            (SystemModules.Finance, Crud)),
    };

    public static IReadOnlyDictionary<string, ModulePermissionFlags> GetForRole(string roleName)
    {
        if (Matrix.TryGetValue(roleName, out var perms))
            return perms;

        return SystemModules.All.ToDictionary(m => m.Key, _ => View, StringComparer.OrdinalIgnoreCase);
    }

    public static IEnumerable<(string ModuleKey, string ModuleName, ModulePermissionFlags Flags)> GetAllForRole(string roleName)
    {
        var perms = GetForRole(roleName);
        foreach (var (key, displayName) in SystemModules.All)
        {
            var flags = perms.TryGetValue(key, out var p) ? p : None;
            yield return (key, displayName, flags);
        }
    }

    private static Dictionary<string, ModulePermissionFlags> Build(params (string Key, ModulePermissionFlags Flags)[] entries)
    {
        var result = SystemModules.All.ToDictionary(m => m.Key, _ => None, StringComparer.OrdinalIgnoreCase);
        foreach (var (key, flags) in entries)
            result[key] = flags;
        return result;
    }
}
