using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SixteenCoreCharacterMapper
{
    public partial class SimpleColorPicker : UserControl
    {
        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register("SelectedColor", typeof(Color), typeof(SimpleColorPicker),
            new PropertyMetadata(Colors.Black, OnSelectedColorChanged));

        public Color SelectedColor
        {
            get { return (Color)GetValue(SelectedColorProperty); }
            set { SetValue(SelectedColorProperty, value); }
        }

        private bool _isUpdatingFromCode = false;

        public SimpleColorPicker()
        {
            InitializeComponent();
        }

        // This new public method will be called by the parent dialog
        public void ApplyTheme(bool isDarkMode)
        {
            if (isDarkMode)
            {
                // Set the background for the entire control in dark mode
                this.Background = new SolidColorBrush(Color.FromRgb(50, 50, 50));
                RLabel.Foreground = Brushes.White;
                GLabel.Foreground = Brushes.White;
                BLabel.Foreground = Brushes.White;
            }
            else
            {
                // Reset the background for light mode
                this.Background = Brushes.Transparent;
                RLabel.Foreground = Brushes.Black;
                GLabel.Foreground = Brushes.Black;
                BLabel.Foreground = Brushes.Black;
            }
        }

        private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var picker = (SimpleColorPicker)d;
            picker._isUpdatingFromCode = true;
            var newColor = (Color)e.NewValue;
            picker.SliderR.Value = newColor.R;
            picker.SliderG.Value = newColor.G;
            picker.SliderB.Value = newColor.B;
            picker._isUpdatingFromCode = false;
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isUpdatingFromCode)
            {
                SelectedColor = Color.FromRgb((byte)SliderR.Value, (byte)SliderG.Value, (byte)SliderB.Value);
            }
        }
    }
}