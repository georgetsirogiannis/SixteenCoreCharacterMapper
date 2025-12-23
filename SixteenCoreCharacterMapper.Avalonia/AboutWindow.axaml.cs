using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using SixteenCoreCharacterMapper.Core;
using SixteenCoreCharacterMapper.Core.Services;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace SixteenCoreCharacterMapper.Avalonia
{
    public partial class AboutWindow : Window
    {
        private readonly IUpdateService _updateService = new UpdateService();

        public AboutWindow()
        {
            InitializeComponent();
            ApplyTheme();
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var versionText = this.FindControl<TextBlock>("VersionText");
            if (versionText != null)
            {
                versionText.Text = $"Version {version?.Major}.{version?.Minor}.{version?.Build}";
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void ApplyTheme()
        {
            var isDark = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
            if (isDark)
            {
                Background = new SolidColorBrush(Color.FromRgb(25, 25, 25));
            }
        }

        private void Ok_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Donate_Click(object? sender, RoutedEventArgs e)
        {
            OpenUrl(AppConstants.DonationUrl);
        }

        private void Email_Click(object? sender, RoutedEventArgs e)
        {
            OpenUrl(AppConstants.ContactEmailUrl);
        }

        private void Website_Click(object? sender, RoutedEventArgs e)
        {
            OpenUrl(AppConstants.WebsiteUrl);
        }

        private async void CheckUpdates_Click(object? sender, RoutedEventArgs e)
        {
            bool isUpdateAvailable = await CheckForUpdatesWithResultAsync();

            if (!isUpdateAvailable)
            {
                var msgBox = new SimpleMessageBox("You're using the latest version available.", "No Updates", SimpleMessageBox.MessageBoxButtons.OK);
                await msgBox.ShowDialog(this);
            }
        }

        private async Task<bool> CheckForUpdatesWithResultAsync()
        {
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0);
            var updateInfo = await _updateService.CheckForUpdateAsync(currentVersion);

            if (updateInfo != null)
            {
                var msgBox = new SimpleMessageBox(
                    $"A new version ({updateInfo.Version}) is available! You are currently using version {currentVersion}.\n\nRelease Notes:\n{updateInfo.ReleaseNotes}\n\nWould you like to go to the download page?",
                    "Update Available",
                    SimpleMessageBox.MessageBoxButtons.YesNo);
                
                await msgBox.ShowDialog(this);

                if (msgBox.Result == Core.Services.DialogResult.Yes)
                {
                    OpenUrl(updateInfo.Url);
                }
                return true;
            }
            
            return false;
        }

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to open URL: {ex.Message}");
            }
        }
    }
}
