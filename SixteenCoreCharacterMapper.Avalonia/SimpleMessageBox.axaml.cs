using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using SixteenCoreCharacterMapper.Core.Services;
using System.Threading.Tasks;

namespace SixteenCoreCharacterMapper.Avalonia
{
    public partial class SimpleMessageBox : Window
    {
        public enum MessageBoxButtons
        {
            OK,
            YesNo,
            YesNoCancel
        }

        public DialogResult Result { get; private set; } = DialogResult.Cancel;

        public SimpleMessageBox()
        {
            InitializeComponent();
            ApplyTheme();
        }

        public SimpleMessageBox(string message, string title, MessageBoxButtons buttons) : this()
        {
            Title = title;
            var messageText = this.FindControl<TextBlock>("MessageText");
            if (messageText != null) messageText.Text = message;

            var buttonsPanel = this.FindControl<StackPanel>("ButtonsPanel");
            if (buttonsPanel != null)
            {
                switch (buttons)
                {
                    case MessageBoxButtons.OK:
                        AddButton(buttonsPanel, "OK", DialogResult.OK, true);
                        break;
                    case MessageBoxButtons.YesNo:
                        AddButton(buttonsPanel, "Yes", DialogResult.Yes, true);
                        AddButton(buttonsPanel, "No", DialogResult.No, false);
                        break;
                    case MessageBoxButtons.YesNoCancel:
                        AddButton(buttonsPanel, "Yes", DialogResult.Yes, true);
                        AddButton(buttonsPanel, "No", DialogResult.No, false);
                        AddButton(buttonsPanel, "Cancel", DialogResult.Cancel, false);
                        break;
                }
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

        private void AddButton(StackPanel panel, string content, DialogResult result, bool isDefault)
        {
            var btn = new Button { Content = content, Width = 60, HorizontalContentAlignment = global::Avalonia.Layout.HorizontalAlignment.Center };
            if (isDefault)
            {
                // Avalonia doesn't have IsDefault property on Button directly in the same way, 
                // but we can set it via attached property or just style.
                // For now, just add it.
                btn.Classes.Add("accent"); 
            }
            
            btn.Click += (s, e) =>
            {
                Result = result;
                Close();
            };
            panel.Children.Add(btn);
        }
    }
}
