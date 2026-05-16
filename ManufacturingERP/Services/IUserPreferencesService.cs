using System.Threading.Tasks;

namespace ManufacturingERP.Services
{
    public interface IUserPreferencesService
    {
        bool IsRememberMe { get; set; }
        string SavedUsername { get; set; }
        string AutoLoginToken { get; set; }
        System.DateTime LastLoginDate { get; set; }
        
        Task SaveAsync();
        void Load();
        void Clear();
    }
}
