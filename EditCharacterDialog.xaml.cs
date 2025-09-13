using System.Windows;
using System.Windows.Media;
using System.Collections.Generic;
using System.Windows.Controls;

namespace SixteenCoreCharacterMapper
{
    public partial class EditCharacterDialog : Window
    {
        public Character? Character { get; private set; }

        // Updated constructor to accept the theme
        public EditCharacterDialog(bool isDarkMode)
        {
            InitializeComponent();
            ApplyTheme(isDarkMode); // Apply theme on creation
            MainRadioButton.IsChecked = true;
        }

        // Updated constructor overload
        public EditCharacterDialog(Character existing, bool isDarkMode) : this(isDarkMode)
        {
            Character = existing;
            NameBox.Text = existing.Name;
            if (!string.IsNullOrEmpty(existing.ColorHex))
            {
                ColorPickerControl.SelectedColor = (Color)ColorConverter.ConvertFromString(existing.ColorHex);
            }

            switch (existing.Size)
            {
                case BubbleSize.Large:
                    MainRadioButton.IsChecked = true;
                    break;
                case BubbleSize.Medium:
                    SupportingRadioButton.IsChecked = true;
                    break;
                case BubbleSize.Small:
                    BackgroundRadioButton.IsChecked = true;
                    break;
            }
        }

        // This new method applies the theme colors to the controls
        private void ApplyTheme(bool isDarkMode)
        {
            ColorPickerControl.ApplyTheme(isDarkMode);

            if (isDarkMode)
            {
                // Dark Theme Colors
                var darkBg = new SolidColorBrush(Color.FromRgb(25, 25, 25));
                var textFg = Brushes.White;
                var controlBg = new SolidColorBrush(Color.FromRgb(50, 50, 50));
                var borderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85));

                Background = darkBg;
                Foreground = textFg;

                PresetColorsList.Background = controlBg;

                // Apply the new dark styles to the TabControl and its items
                ColorTabControl.Style = (Style)FindResource("DarkTabControlStyle");
                ColorTabControl.ItemContainerStyle = (Style)FindResource("DarkTabItemStyle");

                NameLabel.Foreground = textFg;
                ColorLabel.Foreground = textFg;
                CharacterTypeLabel.Foreground = textFg;

                NameBox.Background = controlBg;
                NameBox.Foreground = textFg;
                NameBox.BorderBrush = borderBrush;

                MainRadioButton.Foreground = textFg;
                SupportingRadioButton.Foreground = textFg;
                BackgroundRadioButton.Foreground = textFg;

                OkButton.Style = (Style)FindResource("DarkToolBarButtonStyle");
                CancelButton.Style = (Style)FindResource("DarkToolBarButtonStyle");
            }
            else
            {
                // Light Theme Colors
                PresetColorsList.Background = Brushes.Transparent;

                // Reset tab styles to default for light mode
                ColorTabControl.Style = null;
                ColorTabControl.ItemContainerStyle = null;

                OkButton.Style = (Style)FindResource("ToolBarButtonStyle");
                CancelButton.Style = (Style)FindResource("ToolBarButtonStyle");
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Preset colors list remains unchanged
            var characterColors = new List<ColorItem>()
            {
                new ColorItem(Color.FromRgb(191, 76, 76), "Strong Red"),
                new ColorItem(Color.FromRgb(154, 205, 50), "Yellow Green"),
                new ColorItem(Color.FromRgb(0, 191, 255), "Deep Sky Blue"),
                new ColorItem(Color.FromRgb(106, 90, 205), "Slate Blue"),
                new ColorItem(Color.FromRgb(255, 99, 71), "Tomato"),
                new ColorItem(Color.FromRgb(255, 165, 0), "Orange"),
                new ColorItem(Color.FromRgb(255, 215, 0), "Gold"),
                new ColorItem(Color.FromRgb(50, 205, 50), "Lime Green"),
                new ColorItem(Color.FromRgb(32, 178, 170), "Light Sea Green"),
                new ColorItem(Color.FromRgb(0, 128, 128), "Teal"),
                new ColorItem(Color.FromRgb(70, 130, 180), "Steel Blue"),
                new ColorItem(Color.FromRgb(65, 105, 225), "Royal Blue"),
                new ColorItem(Color.FromRgb(147, 112, 219), "Medium Purple"),
                new ColorItem(Color.FromRgb(218, 112, 214), "Orchid"),
                new ColorItem(Color.FromRgb(199, 21, 133), "Medium Violet Red"),
                new ColorItem(Color.FromRgb(255, 182, 193), "Light Pink"),
                new ColorItem(Color.FromRgb(210, 105, 30), "Chocolate"),
                new ColorItem(Color.FromRgb(240, 128, 128), "Light Coral"),
                new ColorItem(Color.FromRgb(128, 128, 128), "Gray"),
                new ColorItem(Color.FromRgb(255, 250, 240), "Floral White")
            };
            PresetColorsList.ItemsSource = characterColors;
        }

        private void PresetColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton && radioButton.DataContext is ColorItem colorItem)
            {
                ColorPickerControl.SelectedColor = colorItem.Color;
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (Character == null)
                Character = new Character();

            Character.Name = NameBox.Text.Trim();
            Character.ColorHex = ColorPickerControl.SelectedColor.ToString();

            if (MainRadioButton.IsChecked == true)
            {
                Character.Size = BubbleSize.Large;
            }
            else if (SupportingRadioButton.IsChecked == true)
            {
                Character.Size = BubbleSize.Medium;
            }
            else if (BackgroundRadioButton.IsChecked == true)
            {
                Character.Size = BubbleSize.Small;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}