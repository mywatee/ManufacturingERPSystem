using ManufacturingERP.Models;
using System.Threading.Tasks;

namespace ManufacturingERP.Services
{
    public interface IAuthService
    {
        User? CurrentUser { get; }
        Task<bool> LoginAsync(string username, string password);
        void Logout();
        Task<bool> RestoreSessionAsync(string username);
        Task<bool> RequestPasswordResetAsync(string username);
        Task<System.Collections.Generic.List<PasswordResetRequest>> GetPendingResetRequestsAsync();
        /// <summary>
        /// Processes a password reset request.
        /// Returns null if failed/not found/not pending;
        /// returns "" if rejected successfully;
        /// returns a non-empty temporary password if approved successfully.
        /// </summary>
        Task<string?> ProcessResetRequestAsync(int requestId, bool approved, int adminId);
    }
}
