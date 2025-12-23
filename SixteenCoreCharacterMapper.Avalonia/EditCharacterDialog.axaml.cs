using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using SixteenCoreCharacterMapper.Core.Models;
using SixteenCoreCharacterMapper.Core.ViewModels;
using System;

namespace SixteenCoreCharacterMapper.Avalonia
{
    public partial class EditCharacterDialog : Window
    {
        public Character? Character { get; private set; }
        private EditCharacterViewModel? _viewModel;

        public EditCharacterDialog()
        {
            InitializeComponent();
        }

        public EditCharacterDialog(bool isDarkMode) : this()
        {
            Character = new Character();
            _viewModel = new EditCharacterViewModel(Character);
            DataContext = _viewModel;
            ApplyTheme(isDarkMode);
            
            var mainRadio = this.FindControl<RadioButton>("MainRadioButton");
            if (mainRadio != null) mainRadio.IsChecked = true;
        }

        public EditCharacterDialog(Character existing, bool isDarkMode) : this()
        {
            Character = existing;
            _viewModel = new EditCharacterViewModel(Character);
            DataContext = _viewModel;
            ApplyTheme(isDarkMode);

            var colorPicker = this.FindControl<SimpleColorPicker>("ColorPickerControl");
            if (colorPicker != null && !string.IsNullOrEmpty(existing.ColorHex))
            {
                if (Color.TryParse(existing.ColorHex, out var color))
                {
                    colorPicker.SelectedColor = color;
                }
            }

            var mainRadio = this.FindControl<RadioButton>("MainRadioButton");
            var supportingRadio = this.FindControl<RadioButton>("SupportingRadioButton");
            var backgroundRadio = this.FindControl<RadioButton>("BackgroundRadioButton");

            switch (existing.Size)
            {
                case BubbleSize.Large:
                    if (mainRadio != null) mainRadio.IsChecked = true;
                    break;
                case BubbleSize.Medium:
                    if (supportingRadio != null) supportingRadio.IsChecked = true;
                    break;
                case BubbleSize.Small:
                    if (backgroundRadio != null) backgroundRadio.IsChecked = true;
                    break;
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void ApplyTheme(bool isDarkMode)
        {
            var colorPicker = this.FindControl<SimpleColorPicker>("ColorPickerControl");
            colorPicker?.ApplyTheme(isDarkMode);

            var nameLabel = this.FindControl<TextBlock>("NameLabel");
            var colorLabel = this.FindControl<TextBlock>("ColorLabel");
            var typeLabel = this.FindControl<TextBlock>("CharacterTypeLabel");
            var nameBox = this.FindControl<TextBox>("NameBox");
            var presetList = this.FindControl<ItemsControl>("PresetColorsList");
            
            var mainRadio = this.FindControl<RadioButton>("MainRadioButton");
            var supportingRadio = this.FindControl<RadioButton>("SupportingRadioButton");
            var backgroundRadio = this.FindControl<RadioButton>("BackgroundRadioButton");

            var okButton = this.FindControl<Button>("OkButton");
            var cancelButton = this.FindControl<Button>("CancelButton");

            if (isDarkMode)
            {
                Background = new SolidColorBrush(Color.FromRgb(25, 25, 25));
                // Foreground = Brushes.White; // Window foreground not always inherited by all controls in Avalonia?

                if (presetList != null) presetList.Background = new SolidColorBrush(Color.FromRgb(50, 50, 50));

                if (nameLabel != null) nameLabel.Foreground = Brushes.White;
                if (colorLabel != null) colorLabel.Foreground = Brushes.White;
                if (typeLabel != null) typeLabel.Foreground = Brushes.White;

                if (nameBox != null)
                {
                    nameBox.Background = new SolidColorBrush(Color.FromRgb(50, 50, 50));
                    nameBox.Foreground = Brushes.White;
                    nameBox.BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85));
                }

                if (mainRadio != null) mainRadio.Foreground = Brushes.White;
                if (supportingRadio != null) supportingRadio.Foreground = Brushes.White;
                if (backgroundRadio != null) backgroundRadio.Foreground = Brushes.White;

                if (okButton != null) okButton.Foreground = Brushes.White;
                if (cancelButton != null) cancelButton.Foreground = Brushes.White;
            }
            else
            {
                Background = Brushes.White;
                if (presetList != null) presetList.Background = Brushes.Transparent;
                
                if (nameLabel != null) nameLabel.Foreground = Brushes.Black;
                if (colorLabel != null) colorLabel.Foreground = Brushes.Black;
                if (typeLabel != null) typeLabel.Foreground = Brushes.Black;
                
                if (nameBox != null)
                {
                    nameBox.Background = Brushes.White;
                    nameBox.Foreground = Brushes.Black;
                    nameBox.BorderBrush = Brushes.Gray;
                }

                if (mainRadio != null) mainRadio.Foreground = Brushes.Black;
                if (supportingRadio != null) supportingRadio.Foreground = Brushes.Black;
                if (backgroundRadio != null) backgroundRadio.Foreground = Brushes.Black;

                if (okButton != null) okButton.Foreground = Brushes.Black;
                if (cancelButton != null) cancelButton.Foreground = Brushes.Black;
            }
        }

        private void PresetColor_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton && radioButton.DataContext is ColorItem colorItem)
            {
                var colorPicker = this.FindControl<SimpleColorPicker>("ColorPickerControl");
                if (colorPicker != null && Color.TryParse(colorItem.Hex, out var color))
                {
                    colorPicker.SelectedColor = color;
                }
            }
        }

        private void Ok_Click(object? sender, RoutedEventArgs e)
        {
            if (Character == null) return;

            var colorPicker = this.FindControl<SimpleColorPicker>("ColorPickerControl");
            if (colorPicker != null)
            {
                Character.ColorHex = colorPicker.SelectedColor.ToString();
            }

            var mainRadio = this.FindControl<RadioButton>("MainRadioButton");
            var supportingRadio = this.FindControl<RadioButton>("SupportingRadioButton");
            var backgroundRadio = this.FindControl<RadioButton>("BackgroundRadioButton");

            if (mainRadio?.IsChecked == true)
            {
                Character.Size = BubbleSize.Large;
            }
            else if (supportingRadio?.IsChecked == true)
            {
                Character.Size = BubbleSize.Medium;
            }
            else if (backgroundRadio?.IsChecked == true)
            {
                Character.Size = BubbleSize.Small;
            }

            Close(Character);
        }

        private void Cancel_Click(object? sender, RoutedEventArgs e)
        {
            Close(null);
        }
    }
}
