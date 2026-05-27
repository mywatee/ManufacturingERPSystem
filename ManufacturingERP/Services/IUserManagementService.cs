using ManufacturingERP.Models;

namespace ManufacturingERP.Services;

public interface IUserManagementService
{
    Task<List<User>> GetUsersAsync();
    Task<(List<User> Items, int TotalCount)> GetPagedUsersAsync(int page, int pageSize, string? searchTerm = null);
    Task<List<string>> GetRoleNamesAsync();

    Task<User> CreateUserAsync(User user, string roleName, string initialPassword);
    Task<User?> UpdateUserProfileAsync(int userId, string? fullName, string? email, string? phone, string roleName);
    Task<bool> ToggleActiveAsync(int userId, bool isActive);
    Task ResetPasswordAsync(int userId, string newPassword);
    Task DeleteUserAsync(int userId);

    // Role management
    Task<Role> CreateRoleAsync(string roleName);
    Task<bool> RenameRoleAsync(int roleId, string newName);
    Task<bool> DeleteRoleAsync(int roleId);
    Task<List<Role>> GetAllRolesAsync();
}
