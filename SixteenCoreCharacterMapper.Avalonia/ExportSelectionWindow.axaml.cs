using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using SixteenCoreCharacterMapper.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace SixteenCoreCharacterMapper.Avalonia
{
    public class ExportSelectionItem
    {
        public Character Character { get; set; }
        public bool IsSelected { get; set; }

        public ExportSelectionItem(Character character, bool isSelected)
        {
            Character = character;
            IsSelected = isSelected;
        }
    }

    public partial class ExportSelectionWindow : Window
    {
        private List<ExportSelectionItem> _items = new List<ExportSelectionItem>();

        public ExportSelectionWindow()
        {
        }

        public ExportSelectionWindow(IEnumerable<Character> characters, bool isDarkMode) : this()
        {
            _items = characters.Select(c => new ExportSelectionItem(c, true)).ToList();
            var list = this.FindControl<ListBox>("CharactersList");
            if (list != null)
            {
                list.ItemsSource = _items;
                list.IsEnabled = false; // Default to All Characters
                list.Opacity = 0.5;
            }

            ApplyTheme(isDarkMode);
        }

        private void Radio_Checked(object? sender, RoutedEventArgs e)
        {
            var selectRadio = this.FindControl<RadioButton>("SelectCharactersRadio");
            var categoryRadio = this.FindControl<RadioButton>("CategoryRadio");
            var categoryCombo = this.FindControl<ComboBox>("CategoryComboBox");
            var list = this.FindControl<ListBox>("CharactersList");
            
            if (categoryCombo != null && categoryRadio != null)
            {
                categoryCombo.IsEnabled = categoryRadio.IsChecked == true;
            }

            if (list != null && selectRadio != null)
            {
                list.IsEnabled = selectRadio.IsChecked == true;
                
                // Visual feedback for disabled state
                if (list.IsEnabled)
                {
                    list.Opacity = 1.0;
                }
                else
                {
                    list.Opacity = 0.5;
                }
            }
        }

        private void Cancel_Click(object? sender, RoutedEventArgs e)
        {
            Close(null);
        }

        private void Export_Click(object? sender, RoutedEventArgs e)
        {
            var allRadio = this.FindControl<RadioButton>("AllCharactersRadio");
            var categoryRadio = this.FindControl<RadioButton>("CategoryRadio");
            var categoryCombo = this.FindControl<ComboBox>("CategoryComboBox");
            
            if (allRadio?.IsChecked == true)
            {
                // Return all characters
                Close(_items.Select(i => i.Character).ToList());
            }
            else if (categoryRadio?.IsChecked == true && categoryCombo != null)
            {
                var selectedIndex = categoryCombo.SelectedIndex;
                BubbleSize targetSize = selectedIndex switch
                {
                    0 => BubbleSize.Large,
                    1 => BubbleSize.Medium,
                    2 => BubbleSize.Small,
                    _ => BubbleSize.Large
                };

                var filtered = _items
                    .Select(i => i.Character)
                    .Where(c => c.Size == targetSize)
                    .ToList();
                
                Close(filtered);
            }
            else
            {
                // Return selected characters
                var selected = _items.Where(i => i.IsSelected).Select(i => i.Character).ToList();
                Close(selected);
            }
        }

        private void ApplyTheme(bool isDarkMode)
        {
            var title = this.FindControl<TextBlock>("TitleText");
            var allRadio = this.FindControl<RadioButton>("AllCharactersRadio");
            var categoryRadio = this.FindControl<RadioButton>("CategoryRadio");
            var categoryCombo = this.FindControl<ComboBox>("CategoryComboBox");
            var selectRadio = this.FindControl<RadioButton>("SelectCharactersRadio");
            var listBorder = this.FindControl<Border>("ListBorder");
            var list = this.FindControl<ListBox>("CharactersList");
            var cancelBtn = this.FindControl<Button>("CancelButton");
            var exportBtn = this.FindControl<Button>("ExportButton");

            if (isDarkMode)
            {
                Background = new SolidColorBrush(Color.FromRgb(25, 25, 25));
                if (title != null) title.Foreground = Brushes.White;
                if (allRadio != null) allRadio.Foreground = Brushes.White;
                if (categoryRadio != null) categoryRadio.Foreground = Brushes.White;
                if (selectRadio != null) selectRadio.Foreground = Brushes.White;
                
                if (categoryCombo != null)
                {
                    categoryCombo.Background = new SolidColorBrush(Color.FromRgb(50, 50, 50));
                    categoryCombo.Foreground = Brushes.White;
                    categoryCombo.BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85));
                }

                if (listBorder != null) listBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85));
                if (list != null) 
                {
                    list.Background = new SolidColorBrush(Color.FromRgb(50, 50, 50));
                    list.Foreground = Brushes.White;
                }

                if (cancelBtn != null) cancelBtn.Foreground = Brushes.White;
                if (exportBtn != null) exportBtn.Foreground = Brushes.White;
            }
            else
            {
                Background = Brushes.White;
                if (title != null) title.Foreground = Brushes.Black;
                if (allRadio != null) allRadio.Foreground = Brushes.Black;
                if (categoryRadio != null) categoryRadio.Foreground = Brushes.Black;
                if (selectRadio != null) selectRadio.Foreground = Brushes.Black;

                if (categoryCombo != null)
                {
                    categoryCombo.Background = Brushes.White;
                    categoryCombo.Foreground = Brushes.Black;
                    categoryCombo.BorderBrush = Brushes.Gray;
                }

                if (listBorder != null) listBorder.BorderBrush = Brushes.Gray;
                if (list != null)
                {
                    list.Background = Brushes.White;
                    list.Foreground = Brushes.Black;
                }

                if (cancelBtn != null) cancelBtn.Foreground = Brushes.Black;
                if (exportBtn != null) exportBtn.Foreground = Brushes.Black;
            }
        }
    }
}
