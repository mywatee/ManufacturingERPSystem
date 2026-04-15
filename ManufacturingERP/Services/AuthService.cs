using ManufacturingERP.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ManufacturingERP.Services
{
    public class AuthService : IAuthService
    {
        private readonly ManufacturingContext _context;

        public User? CurrentUser { get; private set; }

        public AuthService(ManufacturingContext context)
        {
            _context = context;
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            // Simple authentication for now. 
            // NOTE: In production, compare PasswordHash using BCrypt.
            var user = await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Username.Trim() == username.Trim() && u.PasswordHash.Trim() == password.Trim());

            if (user != null)
            {
                CurrentUser = user;
                return true;
            }

            return false;
        }

        public void Logout()
        {
            CurrentUser = null;
        }
    }
}
