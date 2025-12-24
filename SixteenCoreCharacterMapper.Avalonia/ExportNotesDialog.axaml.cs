using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace SixteenCoreCharacterMapper.Avalonia
{
    public partial class ExportNotesDialog : Window
    {
        public bool IsRtf { get; private set; }

        public ExportNotesDialog()
        {
            InitializeComponent();
            var okBtn = this.FindControl<Button>("OkBtn");
            if (okBtn != null) okBtn.Click += OkBtn_Click;
            
            var cancelBtn = this.FindControl<Button>("CancelBtn");
            if (cancelBtn != null) cancelBtn.Click += CancelBtn_Click;

            ApplyTheme(Application.Current?.RequestedThemeVariant == ThemeVariant.Dark);
        }

        private void OkBtn_Click(object? sender, RoutedEventArgs e)
        {
            var rtf = this.FindControl<RadioButton>("RtfRadio");
            IsRtf = rtf != null && rtf.IsChecked == true;
            Close(true);
        }

        private void CancelBtn_Click(object? sender, RoutedEventArgs e)
        {
            IsRtf = false;
            Close(false);
        }

        public new Task<bool?> ShowDialog(Window owner)
        {
            return base.ShowDialog<bool?>(owner);
        }

        private void ApplyTheme(bool isDarkMode)
        {
            var title = this.FindControl<TextBlock>("TitleText");
            var subtitle = this.FindControl<TextBlock>("SubtitleText");
            var cancelBtn = this.FindControl<Button>("CancelBtn");
            var okBtn = this.FindControl<Button>("OkBtn");

            if (isDarkMode)
            {
                Background = new SolidColorBrush(Color.FromRgb(25, 25, 25));
                if (title != null) title.Foreground = Brushes.White;
                if (subtitle != null) subtitle.Foreground = Brushes.White;
                if (cancelBtn != null) cancelBtn.Foreground = Brushes.White;
                if (okBtn != null) okBtn.Foreground = Brushes.White;
            }
            else
            {
                Background = Brushes.White;
                if (title != null) title.Foreground = Brushes.Black;
                if (subtitle != null) subtitle.Foreground = Brushes.Black;
                if (cancelBtn != null) cancelBtn.Foreground = Brushes.Black;
                if (okBtn != null) okBtn.Foreground = Brushes.Black;
            }
        }
    }
}