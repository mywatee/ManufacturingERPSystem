using System.Threading.Tasks;

namespace ManufacturingERP.Services
{
    public interface IDatabaseSeeder
    {
        Task SeedAsync();
    }
}
