using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System;

namespace SixteenCoreCharacterMapper.Avalonia
{
    public partial class SimpleColorPicker : UserControl
    {
        public static readonly StyledProperty<Color> SelectedColorProperty =
            AvaloniaProperty.Register<SimpleColorPicker, Color>(nameof(SelectedColor), Colors.Black);

        public Color SelectedColor
        {
            get => GetValue(SelectedColorProperty);
            set => SetValue(SelectedColorProperty, value);
        }

        private bool _isUpdatingFromCode = false;

        public SimpleColorPicker()
        {
            InitializeComponent();

            var sliderR = this.FindControl<Slider>("SliderR");
            var sliderG = this.FindControl<Slider>("SliderG");
            var sliderB = this.FindControl<Slider>("SliderB");
            var hexInput = this.FindControl<TextBox>("HexInput");

            if (sliderR != null) sliderR.ValueChanged += Slider_ValueChanged;
            if (sliderG != null) sliderG.ValueChanged += Slider_ValueChanged;
            if (sliderB != null) sliderB.ValueChanged += Slider_ValueChanged;

            if (hexInput != null)
            {
                hexInput.TextChanged += HexInput_TextChanged;
                hexInput.LostFocus += (s, e) => UpdateColorFromHex();
                hexInput.KeyDown += (s, e) =>
                {
                    if (e.Key == Key.Enter) UpdateColorFromHex();
                };
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == SelectedColorProperty)
            {
                _isUpdatingFromCode = true;
                var newColor = change.GetNewValue<Color>();

                var sliderR = this.FindControl<Slider>("SliderR");
                var sliderG = this.FindControl<Slider>("SliderG");
                var sliderB = this.FindControl<Slider>("SliderB");
                var hexInput = this.FindControl<TextBox>("HexInput");

                if (sliderR != null) sliderR.Value = newColor.R;
                if (sliderG != null) sliderG.Value = newColor.G;
                if (sliderB != null) sliderB.Value = newColor.B;

                if (hexInput != null)
                {
                    hexInput.Text = $"{newColor.R:X2}{newColor.G:X2}{newColor.B:X2}";
                }

                _isUpdatingFromCode = false;
            }
        }

        private void HexInput_TextChanged(object? sender, TextChangedEventArgs e)
        {
            if (_isUpdatingFromCode) return;
            if (sender is TextBox textBox && textBox.Text?.Length == 6)
            {
                UpdateColorFromHex();
            }
        }

        private void UpdateColorFromHex()
        {
            if (_isUpdatingFromCode) return;

            var hexInput = this.FindControl<TextBox>("HexInput");
            if (hexInput == null || string.IsNullOrWhiteSpace(hexInput.Text)) return;

            string rawText = hexInput.Text.Trim();
            if (!rawText.StartsWith("#")) rawText = "#" + rawText;

            if (rawText.Length == 4 || rawText.Length == 7)
            {
                if (Color.TryParse(rawText, out var parsedColor))
                {
                    _isUpdatingFromCode = true;
                    SelectedColor = parsedColor;
                    _isUpdatingFromCode = false;
                }
            }
        }

        private void Slider_ValueChanged(object? sender, global::Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (!_isUpdatingFromCode)
            {
                var sliderR = this.FindControl<Slider>("SliderR");
                var sliderG = this.FindControl<Slider>("SliderG");
                var sliderB = this.FindControl<Slider>("SliderB");

                if (sliderR != null && sliderG != null && sliderB != null)
                {
                    SelectedColor = Color.FromRgb((byte)sliderR.Value, (byte)sliderG.Value, (byte)sliderB.Value);
                }
            }
        }

        public void ApplyTheme(bool isDarkMode)
        {
            var rLabel = this.FindControl<TextBlock>("RLabel");
            var gLabel = this.FindControl<TextBlock>("GLabel");
            var bLabel = this.FindControl<TextBlock>("BLabel");
            var hexLabel = this.FindControl<TextBlock>("HexLabel");

            var color = isDarkMode ? Brushes.White : Brushes.Black;
            this.Background = isDarkMode ? new SolidColorBrush(Color.FromRgb(50, 50, 50)) : Brushes.Transparent;

            if (rLabel != null) rLabel.Foreground = color;
            if (gLabel != null) gLabel.Foreground = color;
            if (bLabel != null) bLabel.Foreground = color;
            if (hexLabel != null) hexLabel.Foreground = color;
        }
    }
}