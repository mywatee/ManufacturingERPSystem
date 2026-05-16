using ManufacturingERP.Core;
using ManufacturingERP.Models;
using Microsoft.EntityFrameworkCore;

namespace ManufacturingERP.Services;

public class UserManagementService : IUserManagementService
{
    private readonly IDbContextFactory<ManufacturingContext> _contextFactory;
    private readonly ISettingsService _settingsService;
    private readonly PasswordHasherFactory _hasherFactory;

    public UserManagementService(
        IDbContextFactory<ManufacturingContext> contextFactory,
        ISettingsService settingsService,
        PasswordHasherFactory hasherFactory)
    {
        _contextFactory = contextFactory;
        _settingsService = settingsService;
        _hasherFactory = hasherFactory;
    }

    public async Task<List<User>> GetUsersAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Users
            .AsNoTracking()
            .Include(u => u.Roles)
            .Include(u => u.Employee)
            .OrderBy(u => u.UserId)
            .ToListAsync();
    }
    public async Task<(List<User> Items, int TotalCount)> GetPagedUsersAsync(int page, int pageSize, string? searchTerm = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Users.AsNoTracking()
            .Include(u => u.Roles)
            .Include(u => u.Employee)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchLower = searchTerm.ToLower();
            query = query.Where(u => 
                u.Username.ToLower().Contains(searchLower) ||
                (u.Employee != null && (
                    u.Employee.FullName.ToLower().Contains(searchLower) ||
                    (u.Employee.Email != null && u.Employee.Email.ToLower().Contains(searchLower)) ||
                    (u.Employee.Phone != null && u.Employee.Phone.ToLower().Contains(searchLower))
                ))
            );
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(u => u.UserId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
    public async Task<List<string>> GetRoleNamesAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Roles
            .AsNoTracking()
            .OrderBy(r => r.RoleName)
            .Select(r => r.RoleName)
            .ToListAsync();
    }

    private async Task ValidatePasswordAsync(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Mật khẩu không được trống.");

        var minLen = await _settingsService.GetSettingIntAsync("MinPasswordLength", 8);
        if (password.Length < minLen)
            throw new ArgumentException($"Mật khẩu phải có ít nhất {minLen} ký tự.");

        var isComplexityRequired = await _settingsService.GetSettingBoolAsync("IsComplexityRequired", true);
        if (isComplexityRequired)
        {
            bool hasUpper = password.Any(char.IsUpper);
            bool hasLower = password.Any(char.IsLower);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSpecial = password.Any(ch => !char.IsLetterOrDigit(ch));

            if (!hasUpper || !hasLower || !hasDigit || !hasSpecial)
            {
                throw new ArgumentException("Mật khẩu phải bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.");
            }
        }
    }

    public async Task<User> CreateUserAsync(User user, string roleName, string initialPassword)
    {
        if (string.IsNullOrWhiteSpace(user.Username))
            throw new ArgumentException("Username không được trống.");

        await ValidatePasswordAsync(initialPassword);

        using var context = await _contextFactory.CreateDbContextAsync();

        var exists = await context.Users.AnyAsync(u => u.Username == user.Username);
        if (exists)
            throw new InvalidOperationException("Tên đăng nhập đã tồn tại.");

        var role = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
        if (role == null)
            throw new InvalidOperationException("Vai trò không hợp lệ.");

        var algorithm = await _settingsService.GetSettingAsync("HashAlgorithm", "bcrypt (Khuyến nghị)");
        var hasher = _hasherFactory.GetHasherByName(algorithm);

        var newUser = new User
        {
            Username = user.Username.Trim(),
            PasswordHash = hasher.HashPassword(initialPassword),
            EmployeeId = user.EmployeeId,
            IsActive = user.IsActive ?? true,
            CreatedAt = DateTime.Now
        };
        newUser.Roles.Add(role);

        context.Users.Add(newUser);
        await context.SaveChangesAsync();

        return newUser;
    }

    public async Task<User?> UpdateUserProfileAsync(int userId, string? fullName, string? email, string? phone, string roleName)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var dbUser = await context.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.UserId == userId);
        if (dbUser == null) return null;

        if (dbUser.Employee != null)
        {
            dbUser.Employee.FullName = fullName ?? dbUser.Employee.FullName;
            dbUser.Employee.Email = email;
            dbUser.Employee.Phone = phone;
        }

        var newRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
        if (newRole == null)
            throw new InvalidOperationException("Vai trò không hợp lệ.");

        dbUser.Roles.Clear();
        dbUser.Roles.Add(newRole);

        await context.SaveChangesAsync();
        return dbUser;
    }

    public async Task<bool> ToggleActiveAsync(int userId, bool isActive)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var dbUser = await context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (dbUser == null) return false;
        dbUser.IsActive = isActive;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task ResetPasswordAsync(int userId, string newPassword)
    {
        await ValidatePasswordAsync(newPassword);

        var algorithm = await _settingsService.GetSettingAsync("HashAlgorithm", "bcrypt (Khuyến nghị)");
        var hasher = _hasherFactory.GetHasherByName(algorithm);
        var newHash = hasher.HashPassword(newPassword);

        using var context = await _contextFactory.CreateDbContextAsync();
        var dbUser = await context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (dbUser == null) return;
        dbUser.PasswordHash = newHash;
        await context.SaveChangesAsync();
    }

    public async Task DeleteUserAsync(int userId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var dbUser = await context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (dbUser == null) return;

        bool hasLogs = await context.AuditLogs.AnyAsync(a => a.UserId == userId);
        if (hasLogs)
        {
            throw new InvalidOperationException("Không thể xóa người dùng này vì đã phát sinh dữ liệu hệ thống (nhật ký, giao dịch). Vui lòng sử dụng tính năng 'Khóa tài khoản'.");
        }

        context.Users.Remove(dbUser);
        await context.SaveChangesAsync();
    }
}
