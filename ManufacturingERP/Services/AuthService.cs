using ManufacturingERP.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using ManufacturingERP.Core;

namespace ManufacturingERP.Services
{
    public class AuthService : IAuthService
    {
        private readonly IDbContextFactory<ManufacturingContext> _contextFactory;
        private readonly ISettingsService _settingsService;
        private readonly PasswordHasherFactory _hasherFactory;

        public User? CurrentUser { get; private set; }

        public AuthService(
            IDbContextFactory<ManufacturingContext> contextFactory, 
            ISettingsService settingsService,
            PasswordHasherFactory hasherFactory)
        {
            _contextFactory = contextFactory;
            _settingsService = settingsService;
            _hasherFactory = hasherFactory;
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var user = await context.Users
                .Include(u => u.Roles)
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null || user.IsActive == false)
            {
                return false;
            }

            // Check if locked out
            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.Now)
            {
                var remainingMinutes = (int)Math.Ceiling((user.LockoutEnd.Value - DateTime.Now).TotalMinutes);
                throw new InvalidOperationException($"Tài khoản đã bị khóa do nhập sai nhiều lần. Vui lòng thử lại sau {remainingMinutes} phút.");
            }

            // 1. Identify the correct hasher for the stored hash
            var storedHasher = _hasherFactory.GetHasherForHash(user.PasswordHash);
            
            // 2. Verify password
            bool isValid = false;
            try 
            {
                isValid = storedHasher.VerifyPassword(password, user.PasswordHash);
            }
            catch
            {
                // Fallback for legacy plaintext (if any)
                if (user.PasswordHash == password) isValid = true;
            }

            if (isValid)
            {
                // Reset failed attempts
                user.FailedLoginAttempts = 0;
                user.LockoutEnd = null;

                // 3. Check if the algorithm needs an upgrade
                string targetAlgorithm = await _settingsService.GetSettingAsync("HashAlgorithm", "bcrypt (Khuyến nghị)");
                var targetHasher = _hasherFactory.GetHasherByName(targetAlgorithm);

                if (storedHasher.AlgorithmName != targetHasher.AlgorithmName)
                {
                    // Upgrade password hash to the new algorithm
                    user.PasswordHash = targetHasher.HashPassword(password);
                }

                await context.SaveChangesAsync();

                CurrentUser = user; // Note: user is now detached
                return true;
            }
            else
            {
                // Increment failed attempts
                user.FailedLoginAttempts++;
                int maxAttempts = await _settingsService.GetSettingIntAsync("MaxLoginAttempts", 5);
                
                if (user.FailedLoginAttempts >= maxAttempts)
                {
                    int lockoutDuration = 15; // default 15 minutes
                    user.LockoutEnd = DateTime.Now.AddMinutes(lockoutDuration);
                    await context.SaveChangesAsync();
                    throw new InvalidOperationException($"Bạn đã nhập sai mật khẩu {user.FailedLoginAttempts} lần. Tài khoản bị khóa {lockoutDuration} phút.");
                }
                
                await context.SaveChangesAsync();
                return false;
            }

            return false;
        }

        public void Logout()
        {
            CurrentUser = null;
        }

        public async Task<bool> RestoreSessionAsync(string username)
        {
            if (string.IsNullOrEmpty(username)) return false;

            using var context = await _contextFactory.CreateDbContextAsync();
            var user = await context.Users
                .Include(u => u.Roles)
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user != null && user.IsActive != false)
            {
                CurrentUser = user;
                return true;
            }

            return false;
        }

        public async Task<bool> RequestPasswordResetAsync(string username)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var user = await context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return false;

            // Check if there's already a pending request
            var existingRequest = await context.PasswordResetRequests
                .FirstOrDefaultAsync(r => r.UserId == user.UserId && r.Status == "Pending");

            if (existingRequest != null) return true; // Already requested

            var request = new PasswordResetRequest
            {
                UserId = user.UserId,
                Username = user.Username,
                RequestDate = System.DateTime.Now,
                Status = "Pending"
            };

            context.PasswordResetRequests.Add(request);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<System.Collections.Generic.List<PasswordResetRequest>> GetPendingResetRequestsAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.PasswordResetRequests
                .Include(r => r.User)
                .Where(r => r.Status == "Pending")
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();
        }

        public async Task<string?> ProcessResetRequestAsync(int requestId, bool approved, int adminId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var request = await context.PasswordResetRequests.FindAsync(requestId);
            if (request == null || request.Status != "Pending") return null;

            request.Status = approved ? "Approved" : "Rejected";
            request.ProcessedDate = System.DateTime.Now;
            request.ProcessedByAdminId = adminId;

            // null => failure; "" => rejected success; non-empty => approved temp password
            string tempPassword = string.Empty;
            if (approved)
            {
                var user = await context.Users.FindAsync(request.UserId);
                if (user != null)
                {
                    // Reset to a temporary password (avoid hard-coded defaults)
                    string targetAlgorithm = await _settingsService.GetSettingAsync("HashAlgorithm", "bcrypt (Khuyến nghị)");
                    var hasher = _hasherFactory.GetHasherByName(targetAlgorithm);
                    tempPassword = PasswordGenerator.GenerateTemporaryPassword();
                    user.PasswordHash = hasher.HashPassword(tempPassword);
                }
            }

            await context.SaveChangesAsync();
            return approved ? tempPassword : string.Empty;
        }
    }
}
