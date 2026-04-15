using ManufacturingERP.Models;
using System.Threading.Tasks;

namespace ManufacturingERP.Services
{
    public interface IAuthService
    {
        User? CurrentUser { get; }
        Task<bool> LoginAsync(string username, string password);
        void Logout();
    }
}
