using ManufacturingERP.Core;
using ManufacturingERP.Models;
using Microsoft.EntityFrameworkCore;

namespace ManufacturingERP.Services;

public class PermissionService : IPermissionService
{
    private readonly IDbContextFactory<ManufacturingContext> _contextFactory;

    public PermissionService(IDbContextFactory<ManufacturingContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<RolePermissionDto>> GetRolePermissionsAsync(string roleName)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var role = await context.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.RoleName == roleName);
        if (role == null)
        {
            return RolePermissionDefaults.GetAllForRole(roleName)
                .Select(x => new RolePermissionDto(x.ModuleKey, x.ModuleName, x.Flags.CanView, x.Flags.CanAdd, x.Flags.CanEdit, x.Flags.CanDelete))
                .ToList();
        }

        var existing = await context.RolePermissions
            .AsNoTracking()
            .Where(p => p.RoleId == role.RoleId)
            .ToListAsync();

        var defaults = RolePermissionDefaults.GetForRole(roleName);
        var byKey = existing.ToDictionary(p => p.ModuleKey, StringComparer.OrdinalIgnoreCase);
        var result = new List<RolePermissionDto>(SystemModules.All.Length);
        foreach (var (key, displayName) in SystemModules.All)
        {
            if (byKey.TryGetValue(key, out var p))
            {
                result.Add(new RolePermissionDto(key, displayName, p.CanView, p.CanAdd, p.CanEdit, p.CanDelete));
            }
            else if (defaults.TryGetValue(key, out var fallback))
            {
                result.Add(new RolePermissionDto(key, displayName, fallback.CanView, fallback.CanAdd, fallback.CanEdit, fallback.CanDelete));
            }
            else
            {
                result.Add(new RolePermissionDto(key, displayName, CanView: false, CanAdd: false, CanEdit: false, CanDelete: false));
            }
        }

        return result;
    }

    public async Task<Dictionary<string, RolePermissionDto>> GetUserPermissionsAsync(int userId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var user = await context.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.UserId == userId);
        
        var result = new Dictionary<string, RolePermissionDto>(StringComparer.OrdinalIgnoreCase);
        
        // Khởi tạo tất cả modules với quyền false
        foreach (var (key, displayName) in SystemModules.All)
        {
            result[key] = new RolePermissionDto(key, displayName, false, false, false, false);
        }

        if (user == null || !user.Roles.Any()) return result;

        var roleIds = user.Roles.Select(r => r.RoleId).ToList();
        
        var permissions = await context.RolePermissions
            .AsNoTracking()
            .Where(p => roleIds.Contains(p.RoleId))
            .ToListAsync();

        // Gộp quyền từ nhiều vai trò (chỉ cần 1 vai trò có quyền là được)
        foreach (var p in permissions)
        {
            if (result.TryGetValue(p.ModuleKey, out var current))
            {
                result[p.ModuleKey] = new RolePermissionDto(
                    p.ModuleKey, 
                    current.ModuleName,
                    current.CanView || p.CanView,
                    current.CanAdd || p.CanAdd,
                    current.CanEdit || p.CanEdit,
                    current.CanDelete || p.CanDelete
                );
            }
        }

        return result;
    }

    public async Task SaveRolePermissionsAsync(string roleName, IEnumerable<RolePermissionDto> permissions)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var role = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
        if (role == null) throw new InvalidOperationException("Vai trò không hợp lệ.");

        var desired = permissions.ToDictionary(p => p.ModuleKey, StringComparer.OrdinalIgnoreCase);

        var existing = await context.RolePermissions
            .Where(p => p.RoleId == role.RoleId)
            .ToListAsync();

        foreach (var (key, displayName) in SystemModules.All)
        {
            if (!desired.TryGetValue(key, out var dto))
                continue;

            var row = existing.FirstOrDefault(p => p.ModuleKey.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (row == null)
            {
                context.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.RoleId,
                    ModuleKey = key,
                    CanView = dto.CanView,
                    CanAdd = dto.CanAdd,
                    CanEdit = dto.CanEdit,
                    CanDelete = dto.CanDelete
                });
            }
            else
            {
                row.CanView = dto.CanView;
                row.CanAdd = dto.CanAdd;
                row.CanEdit = dto.CanEdit;
                row.CanDelete = dto.CanDelete;
            }
        }

        await context.SaveChangesAsync();
    }

    public async Task ResetRolePermissionsAsync(string roleName)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var role = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
        if (role == null) return;

        var rows = await context.RolePermissions.Where(p => p.RoleId == role.RoleId).ToListAsync();
        if (rows.Count == 0) return;

        var defaultPerms = RolePermissionDefaults.GetForRole(roleName);

        foreach (var row in rows)
        {
            if (defaultPerms.TryGetValue(row.ModuleKey, out var flags))
            {
                row.CanView = flags.CanView;
                row.CanAdd = flags.CanAdd;
                row.CanEdit = flags.CanEdit;
                row.CanDelete = flags.CanDelete;
            }
            else
            {
                row.CanView = false;
                row.CanAdd = false;
                row.CanEdit = false;
                row.CanDelete = false;
            }
        }

        await context.SaveChangesAsync();
    }
}

