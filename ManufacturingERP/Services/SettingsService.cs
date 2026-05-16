using System;
using System.Linq;
using System.Threading.Tasks;
using ManufacturingERP.Models;
using Microsoft.EntityFrameworkCore;

namespace ManufacturingERP.Services;

public class SettingsService : ISettingsService
{
    private readonly IDbContextFactory<ManufacturingContext> _contextFactory;

    public SettingsService(IDbContextFactory<ManufacturingContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<string> GetSettingAsync(string key, string defaultValue = "")
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var setting = await context.SystemSettings
            .FirstOrDefaultAsync(s => s.SettingKey == key);
        return setting?.SettingValue ?? defaultValue;
    }

    public async Task<int> GetSettingIntAsync(string key, int defaultValue = 0)
    {
        var value = await GetSettingAsync(key);
        return int.TryParse(value, out int result) ? result : defaultValue;
    }

    public async Task<bool> GetSettingBoolAsync(string key, bool defaultValue = false)
    {
        var value = await GetSettingAsync(key);
        return bool.TryParse(value, out bool result) ? result : defaultValue;
    }

    public async Task SetSettingAsync(string key, string value)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var setting = await context.SystemSettings
            .FirstOrDefaultAsync(s => s.SettingKey == key);

        if (setting == null)
        {
            setting = new SystemSetting { SettingKey = key, SettingValue = value };
            context.SystemSettings.Add(setting);
        }
        else
        {
            setting.SettingValue = value;
            setting.LastUpdated = DateTime.Now;
        }

        await context.SaveChangesAsync();
    }

    public async Task SetSettingAsync(string key, int value) => await SetSettingAsync(key, value.ToString());

    public async Task SetSettingAsync(string key, bool value) => await SetSettingAsync(key, value.ToString());
}
