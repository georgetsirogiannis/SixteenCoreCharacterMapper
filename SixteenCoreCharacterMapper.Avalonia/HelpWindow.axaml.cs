using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;

namespace SixteenCoreCharacterMapper.Avalonia
{
    public partial class HelpWindow : Window
    {
        public HelpWindow()
        {
            InitializeComponent();
            ApplyTheme();
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
            else
            {
                Background = Brushes.White;
            }
        }

        private void Close_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
