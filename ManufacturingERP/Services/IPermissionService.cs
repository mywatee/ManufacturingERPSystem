namespace ManufacturingERP.Services;

public record RolePermissionDto(
    string ModuleKey,
    string ModuleName,
    bool CanView,
    bool CanAdd,
    bool CanEdit,
    bool CanDelete);

public interface IPermissionService
{
    Task<List<RolePermissionDto>> GetRolePermissionsAsync(string roleName);
    Task<Dictionary<string, RolePermissionDto>> GetUserPermissionsAsync(int userId);
    Task SaveRolePermissionsAsync(string roleName, IEnumerable<RolePermissionDto> permissions);
    Task ResetRolePermissionsAsync(string roleName);
}

