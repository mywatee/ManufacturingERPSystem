using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace ManufacturingERP.Services
{
    public class UserPreferencesService : IUserPreferencesService
    {
        private readonly string _filePath;

        public bool IsRememberMe { get; set; }
        public string SavedUsername { get; set; } = string.Empty;
        public string AutoLoginToken { get; set; } = string.Empty;
        public DateTime LastLoginDate { get; set; }

        public UserPreferencesService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string folderPath = Path.Combine(appData, "ManufacturingERP");
            
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            _filePath = Path.Combine(folderPath, "user_preferences.json");
            Load();
        }

        public async Task SaveAsync()
        {
            try
            {
                var data = new
                {
                    IsRememberMe,
                    SavedUsername,
                    AutoLoginToken,
                    LastLoginDate
                };

                string json = JsonSerializer.Serialize(data);
                await File.WriteAllTextAsync(_filePath, json);
            }
            catch
            {
                // Silently fail or log
            }
        }

        public void Load()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    string json = File.ReadAllText(_filePath);
                    var data = JsonSerializer.Deserialize<JsonData>(json);
                    
                    if (data != null)
                    {
                        IsRememberMe = data.IsRememberMe;
                        SavedUsername = data.SavedUsername ?? string.Empty;
                        AutoLoginToken = data.AutoLoginToken ?? string.Empty;
                        LastLoginDate = data.LastLoginDate;
                    }
                }
            }
            catch
            {
                // Return defaults
            }
        }

        public void Clear()
        {
            IsRememberMe = false;
            SavedUsername = string.Empty;
            AutoLoginToken = string.Empty;
            LastLoginDate = default;
            
            if (File.Exists(_filePath))
            {
                try { File.Delete(_filePath); } catch { }
            }
        }

        private class JsonData
        {
            public bool IsRememberMe { get; set; }
            public string? SavedUsername { get; set; }
            public string? AutoLoginToken { get; set; }
            public DateTime LastLoginDate { get; set; }
        }
    }
}
