using System.Globalization;
using System.Resources;
using SixteenCoreCharacterMapper.Core.Properties;

namespace SixteenCoreCharacterMapper.Core.Services
{
    public class LocalizationService
    {
        private ResourceManager _resourceManager;
        private CultureInfo _currentCulture;

        public LocalizationService()
        {
            _resourceManager = new ResourceManager("SixteenCoreCharacterMapper.Core.Properties.Strings", typeof(LocalizationService).Assembly);
            _currentCulture = CultureInfo.CurrentUICulture;
        }

        public string GetString(string key)
        {
            var str = _resourceManager.GetString(key, _currentCulture);
            return str ?? string.Empty;
        }

        public void SetCulture(string cultureCode)
        {
            if (string.IsNullOrEmpty(cultureCode)) return;
            _currentCulture = new CultureInfo(cultureCode);

            CultureInfo.CurrentUICulture = _currentCulture;
            CultureInfo.CurrentCulture = _currentCulture;
            CultureInfo.DefaultThreadCurrentCulture = _currentCulture;
            CultureInfo.DefaultThreadCurrentUICulture = _currentCulture;

            try
            {
                Strings.Culture = _currentCulture;
            }
            catch { /* Ignore if not present */ }
        }
    }
}
