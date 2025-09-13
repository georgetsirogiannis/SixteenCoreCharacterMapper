using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace SixteenCoreCharacterMapper
{
    public enum BubbleSize
    {
        Small = 0,
        Medium = 1,
        Large = 2
    }

    public class Character : INotifyPropertyChanged
    {
        // Private backing fields for properties that need to notify the UI of changes.
        private string _name = string.Empty;
        private string _colorHex = "#FF0000";
        private BubbleSize _size = BubbleSize.Large;
        private bool _isLocked = false;
        private bool _isVisible = true;
        private int _displayOrder;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        public string ColorHex
        {
            get => _colorHex;
            set
            {
                if (_colorHex != value)
                {
                    _colorHex = value;
                    OnPropertyChanged(nameof(ColorHex));
                }
            }
        }

        public BubbleSize Size
        {
            get => _size;
            set
            {
                if (_size != value)
                {
                    _size = value;
                    OnPropertyChanged(nameof(Size));
                }
            }
        }

        public bool IsLocked
        {
            get => _isLocked;
            set
            {
                if (_isLocked != value)
                {
                    _isLocked = value;
                    OnPropertyChanged(nameof(IsLocked));
                }
            }
        }

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    OnPropertyChanged(nameof(IsVisible));
                }
            }
        }

        public int DisplayOrder
        {
            get => _displayOrder;
            set
            {
                if (_displayOrder != value)
                {
                    _displayOrder = value;
                    OnPropertyChanged(nameof(DisplayOrder));
                }
            }
        }

        // Trait name → position (0.0 to 1.0)
        public Dictionary<string, double> TraitPositions { get; set; } = new Dictionary<string, double>();

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
