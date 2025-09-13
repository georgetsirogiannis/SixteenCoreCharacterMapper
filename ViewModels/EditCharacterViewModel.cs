using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;

namespace SixteenCoreCharacterMapper.ViewModels
{
    public class EditCharacterViewModel : ViewModelBase
    {
        private Character _character;
        private SolidColorBrush _selectedColorBrush;

        public Character Character
        {
            get => _character;
            set => SetProperty(ref _character, value);
        }

        public string Name
        {
            get => Character.Name;
            set
            {
                if (Character.Name != value)
                {
                    Character.Name = value;
                    OnPropertyChanged();
                }
            }
        }

        public SolidColorBrush SelectedColorBrush
        {
            get => _selectedColorBrush;
            set => SetProperty(ref _selectedColorBrush, value);
        }

        public List<SolidColorBrush> AvailableColors { get; }

        public ICommand SelectColorCommand { get; }

        public bool IsMainCharacter
        {
            get => Character.Size == BubbleSize.Large;
            set
            {
                if (value)
                {
                    Character.Size = BubbleSize.Large;
                    OnPropertyChanged(nameof(IsMainCharacter));
                    OnPropertyChanged(nameof(IsSupportingCharacter));
                    OnPropertyChanged(nameof(IsBackgroundCharacter));
                }
            }
        }

        public bool IsSupportingCharacter
        {
            get => Character.Size == BubbleSize.Medium;
            set
            {
                if (value)
                {
                    Character.Size = BubbleSize.Medium;
                    OnPropertyChanged(nameof(IsMainCharacter));
                    OnPropertyChanged(nameof(IsSupportingCharacter));
                    OnPropertyChanged(nameof(IsBackgroundCharacter));
                }
            }
        }

        public bool IsBackgroundCharacter
        {
            get => Character.Size == BubbleSize.Small;
            set
            {
                if (value)
                {
                    Character.Size = BubbleSize.Small;
                    OnPropertyChanged(nameof(IsMainCharacter));
                    OnPropertyChanged(nameof(IsSupportingCharacter));
                    OnPropertyChanged(nameof(IsBackgroundCharacter));
                }
            }
        }

        public EditCharacterViewModel(Character character)
        {
            _character = character;

            AvailableColors = new List<SolidColorBrush>
            {
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BF4C4C")),
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9ACD32")),
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00BFFF")),
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6A5ACD")),
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6347")),
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFA500")),
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD700")),
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#32CD32")),
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#20B2AA")),
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#008080")),
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4682B4")),
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4169E1")),
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9370DB")),
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DA70D6")),
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C71585")),
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFB6C1")),
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D2691E")),
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F08080")),
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#808080")),
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF5EE"))
            };

            if (!string.IsNullOrEmpty(Character.ColorHex))
            {
                _selectedColorBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Character.ColorHex));
            }
            else
            {
                _selectedColorBrush = AvailableColors.First();
                Character.ColorHex = _selectedColorBrush.Color.ToString();
            }


            SelectColorCommand = new RelayCommand<SolidColorBrush>(SelectColor);
        }

        private void SelectColor(SolidColorBrush? brush)
        {
            if (brush != null)
            {
                SelectedColorBrush = brush;
                Character.ColorHex = brush.Color.ToString();
            }
        }
    }
}
