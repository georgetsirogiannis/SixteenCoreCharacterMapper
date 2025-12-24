using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using SixteenCoreCharacterMapper.Core.Models;

namespace SixteenCoreCharacterMapper.Core.Services
{
    public class SettingsService
    {
        private readonly string _settingsFilePath;

        public SettingsService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(appData, "SixteenCoreCharacterMapper");
            Directory.CreateDirectory(appFolder);
            _settingsFilePath = Path.Combine(appFolder, "settings.json");
        }

        public AppSettings LoadSettings()
        {
            if (File.Exists(_settingsFilePath))
            {
                try
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null) return settings;
                }
                catch
                {
                    // Ignore errors and return default
                }
            }
            return new AppSettings();
        }

        public async Task<AppSettings> LoadSettingsAsync()
        {
            if (File.Exists(_settingsFilePath))
            {
                try
                {
                    using var stream = new FileStream(_settingsFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
                    var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream);
                    if (settings != null) return settings;
                }
                catch
                {
                    // Ignore errors and return default
                }
            }
            return new AppSettings();
        }

        public void SaveSettings(AppSettings settings)
        {
            try
            {
                var json = JsonSerializer.Serialize(settings);
                File.WriteAllText(_settingsFilePath, json);
            }
            catch
            {
                // Ignore errors
            }
        }

        public async Task SaveSettingsAsync(AppSettings settings)
        {
            try
            {
                using var stream = new FileStream(_settingsFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
                await JsonSerializer.SerializeAsync(stream, settings);
            }
            catch
            {
                // Ignore errors
            }
        }
    }
}
