using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using SixteenCoreCharacterMapper.Core.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SixteenCoreCharacterMapper.Avalonia.Services
{
    public class AvaloniaDialogService : IDialogService
    {
        public void ShowMessage(string message, string title)
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = desktop.MainWindow;
                if (mainWindow != null)
                {
                    var msgBox = new SimpleMessageBox(message, title, SimpleMessageBox.MessageBoxButtons.OK);
                    msgBox.ShowDialog(mainWindow);
                }
            }
        }

        public bool Confirm(string message, string title)
        {
            // Synchronous confirm is not supported well in Avalonia without blocking.
            // We should use ConfirmAsync instead.
            // For compatibility, we might return false or throw.
            return false;
        }

        public async Task<bool> ConfirmAsync(string message, string title)
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = desktop.MainWindow;
                if (mainWindow != null)
                {
                    var msgBox = new SimpleMessageBox(message, title, SimpleMessageBox.MessageBoxButtons.YesNo);
                    await msgBox.ShowDialog(mainWindow);
                    return msgBox.Result == DialogResult.Yes;
                }
            }
            return false;
        }

        public DialogResult AskUnsavedChanges()
        {
            // Same issue as Confirm.
            return DialogResult.No;
        }

        public async Task<DialogResult> AskUnsavedChangesAsync()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = desktop.MainWindow;
                if (mainWindow != null)
                {
                    var msgBox = new SimpleMessageBox("Do you want to save changes to the current project?", "Unsaved Changes", SimpleMessageBox.MessageBoxButtons.YesNoCancel);
                    await msgBox.ShowDialog(mainWindow);
                    return msgBox.Result;
                }
            }
            return DialogResult.Cancel;
        }

        public async Task<string?> OpenFileAsync(string filter)
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = desktop.MainWindow;
                if (mainWindow?.StorageProvider != null)
                {
                    var options = new FilePickerOpenOptions
                    {
                        Title = "Open Project",
                        AllowMultiple = false,
                        FileTypeFilter = new List<FilePickerFileType>
                        {
                            new FilePickerFileType("Character Map") { Patterns = new[] { "*.16core", "*.json" } },
                            new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
                        }
                    };

                    var result = await mainWindow.StorageProvider.OpenFilePickerAsync(options);
                    if (result.Count > 0)
                    {
                        return result[0].Path.LocalPath;
                    }
                }
            }
            return null;
        }

        public async Task<string?> SaveFileAsync(string filter, string defaultName)
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = desktop.MainWindow;
                if (mainWindow?.StorageProvider != null)
                {
                    var options = new FilePickerSaveOptions
                    {
                        Title = "Save Project",
                        SuggestedFileName = defaultName,
                        DefaultExtension = "16core",
                        FileTypeChoices = new List<FilePickerFileType>
                        {
                            new FilePickerFileType("Character Map") { Patterns = new[] { "*.16core" } },
                            new FilePickerFileType("Legacy Character Map") { Patterns = new[] { "*.json" } }
                        }
                    };

                    var result = await mainWindow.StorageProvider.SaveFilePickerAsync(options);
                    if (result != null)
                    {
                        return result.Path.LocalPath;
                    }
                }
            }
            return null;
        }
    }
}
