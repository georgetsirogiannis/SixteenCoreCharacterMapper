using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SixteenCoreCharacterMapper.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace SixteenCoreCharacterMapper.Core.ViewModels
{
    public partial class EditCharacterViewModel : ObservableObject
    {
        [ObservableProperty]
        private Character _character;

        [ObservableProperty]
        private ColorItem _selectedColorItem;

        [ObservableProperty]
        private string _questionnaireButtonText = "Take Questionnaire";

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

        public List<ColorItem> AvailableColors { get; }

        public EditCharacterViewModel(Character character)
        {
            _character = character;
            UpdateQuestionnaireStatus();

            AvailableColors = new List<ColorItem>
            {
                new ColorItem("#BF4C4C", "Red"),
                new ColorItem("#9ACD32", "YellowGreen"),
                new ColorItem("#00BFFF", "DeepSkyBlue"),
                new ColorItem("#6A5ACD", "SlateBlue"),
                new ColorItem("#FF6347", "Tomato"),
                new ColorItem("#FFA500", "Orange"),
                new ColorItem("#FFD700", "Gold"),
                new ColorItem("#32CD32", "LimeGreen"),
                new ColorItem("#20B2AA", "LightSeaGreen"),
                new ColorItem("#008080", "Teal"),
                new ColorItem("#4682B4", "SteelBlue"),
                new ColorItem("#4169E1", "RoyalBlue"),
                new ColorItem("#9370DB", "MediumPurple"),
                new ColorItem("#DA70D6", "Orchid"),
                new ColorItem("#C71585", "MediumVioletRed"),
                new ColorItem("#FFB6C1", "LightPink"),
                new ColorItem("#D2691E", "Chocolate"),
                new ColorItem("#F08080", "LightCoral"),
                new ColorItem("#808080", "Gray"),
                new ColorItem("#FFF5EE", "Seashell")
            };

            if (!string.IsNullOrEmpty(Character.ColorHex))
            {
                _selectedColorItem = AvailableColors.FirstOrDefault(c => c.Hex == Character.ColorHex) 
                                     ?? new ColorItem(Character.ColorHex, "Custom");
            }
            else
            {
                _selectedColorItem = AvailableColors.First();
                Character.ColorHex = _selectedColorItem.Hex;
            }
        }

        public void UpdateQuestionnaireStatus()
        {
            QuestionnaireButtonText = Character.QuestionnaireAnswers.Count > 0 
                ? "Edit Questionnaire" 
                : "Take Questionnaire";
        }

        [RelayCommand]
        private void SelectColor(ColorItem? item)
        {
            if (item != null)
            {
                SelectedColorItem = item;
                Character.ColorHex = item.Hex;
            }
        }

        public bool IsMainCharacter
        {
            get => Character.Size == BubbleSize.Large;
            set
            {
                if (value)
                {
                    Character.Size = BubbleSize.Large;
                    NotifySizeChanged();
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
                    NotifySizeChanged();
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
                    NotifySizeChanged();
                }
            }
        }

        private void NotifySizeChanged()
        {
            OnPropertyChanged(nameof(IsMainCharacter));
            OnPropertyChanged(nameof(IsSupportingCharacter));
            OnPropertyChanged(nameof(IsBackgroundCharacter));
        }
    }
}
