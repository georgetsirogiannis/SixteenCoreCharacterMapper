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

        private void ScrollToSection_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string targetName)
            {
                var target = this.FindControl<Control>(targetName);
                var scrollViewer = this.FindControl<ScrollViewer>("MainScrollViewer");

                if (target != null && scrollViewer != null && scrollViewer.Content is Control content)
                {
                    var offset = target.TranslatePoint(new Point(0, 0), content);
                    if (offset.HasValue)
                    {
                        scrollViewer.Offset = new Vector(0, offset.Value.Y);
                    }
                }
            }
        }
    }
}
