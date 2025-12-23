using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using SixteenCoreCharacterMapper.Core.Models;
using SixteenCoreCharacterMapper.Core.Services;
using System.Threading.Tasks;

namespace SixteenCoreCharacterMapper.Avalonia.Services
{
    public class AvaloniaWindowService : IWindowService
    {
        public async Task<Character?> ShowEditCharacterDialogAsync(Character? character = null, bool isDarkMode = false)
        {
            var dialog = character == null 
                ? new EditCharacterDialog(isDarkMode) 
                : new EditCharacterDialog(character, isDarkMode);

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = desktop.MainWindow;
                if (mainWindow != null)
                {
                    return await dialog.ShowDialog<Character?>(mainWindow);
                }
            }
            
            return null;
        }
    }
}
