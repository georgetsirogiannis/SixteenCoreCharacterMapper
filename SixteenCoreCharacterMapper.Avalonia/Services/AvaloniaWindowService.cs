using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using SixteenCoreCharacterMapper.Core.Models;
using SixteenCoreCharacterMapper.Core.Services;
using SixteenCoreCharacterMapper.Core.ViewModels;
using System.Collections.Generic;
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

        public async Task<(Dictionary<string, double>? Scores, Dictionary<string, int>? Answers, List<string>? Exclusions)> ShowQuestionnaireAsync(Dictionary<string, int>? existingAnswers = null, List<string>? existingExclusions = null, bool isDarkMode = false)
        {
            var service = new QuestionnaireService();
            var vm = new QuestionnaireViewModel(service, existingAnswers, existingExclusions);
            var window = new QuestionnaireWindow(isDarkMode)
            {
                DataContext = vm
            };

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = desktop.MainWindow;
                if (mainWindow != null)
                {
                    return await window.ShowDialog<(Dictionary<string, double>? Scores, Dictionary<string, int>? Answers, List<string>? Exclusions)>(mainWindow);
                }
            }
            return (null, null, null);
        }
    }
}
