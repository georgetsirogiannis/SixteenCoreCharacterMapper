using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SixteenCoreCharacterMapper.Avalonia.Services;
using SixteenCoreCharacterMapper.Core.ViewModels;

namespace SixteenCoreCharacterMapper.Avalonia
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // 1. Create the services
                var dialogService = new AvaloniaDialogService();
                var windowService = new AvaloniaWindowService();

                // 2. Create the ViewModel using those services
                var viewModel = new MainWindowViewModel(dialogService, windowService);

                // 3. Create the Main Window and assign the DataContext
                desktop.MainWindow = new MainWindow
                {
                    DataContext = viewModel
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}