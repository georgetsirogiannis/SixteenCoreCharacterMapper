using System;
using System.IO;
using System.Text.Json;
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
    }
}
