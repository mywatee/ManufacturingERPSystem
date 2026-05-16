using System.Threading.Tasks;

namespace ManufacturingERP.Services;

public interface ISettingsService
{
    Task<string> GetSettingAsync(string key, string defaultValue = "");
    Task<int> GetSettingIntAsync(string key, int defaultValue = 0);
    Task<bool> GetSettingBoolAsync(string key, bool defaultValue = false);
    Task SetSettingAsync(string key, string value);
    Task SetSettingAsync(string key, int value);
    Task SetSettingAsync(string key, bool value);
}
