using Avalonia;
using Avalonia.Controls;
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

            if (sliderR != null) sliderR.ValueChanged += Slider_ValueChanged;
            if (sliderG != null) sliderG.ValueChanged += Slider_ValueChanged;
            if (sliderB != null) sliderB.ValueChanged += Slider_ValueChanged;
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

                if (sliderR != null) sliderR.Value = newColor.R;
                if (sliderG != null) sliderG.Value = newColor.G;
                if (sliderB != null) sliderB.Value = newColor.B;
                
                _isUpdatingFromCode = false;
            }
        }

        public void ApplyTheme(bool isDarkMode)
        {
            var rLabel = this.FindControl<TextBlock>("RLabel");
            var gLabel = this.FindControl<TextBlock>("GLabel");
            var bLabel = this.FindControl<TextBlock>("BLabel");

            if (isDarkMode)
            {
                this.Background = new SolidColorBrush(Color.FromRgb(50, 50, 50));
                if (rLabel != null) rLabel.Foreground = Brushes.White;
                if (gLabel != null) gLabel.Foreground = Brushes.White;
                if (bLabel != null) bLabel.Foreground = Brushes.White;
            }
            else
            {
                this.Background = Brushes.Transparent;
                if (rLabel != null) rLabel.Foreground = Brushes.Black;
                if (gLabel != null) gLabel.Foreground = Brushes.Black;
                if (bLabel != null) bLabel.Foreground = Brushes.Black;
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
    }
}
