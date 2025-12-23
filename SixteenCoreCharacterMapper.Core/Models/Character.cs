using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SixteenCoreCharacterMapper.Core.Models
{
    public enum BubbleSize
    {
        Small = 0,
        Medium = 1,
        Large = 2
    }

    public partial class Character : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _colorHex = "#FF0000";

        [ObservableProperty]
        private BubbleSize _size = BubbleSize.Large;

        [ObservableProperty]
        private bool _isLocked = false;

        [ObservableProperty]
        private bool _isVisible = true;

        [ObservableProperty]
        private int _displayOrder;

        // Trait name ? position (0.0 to 1.0)
        public Dictionary<string, double> TraitPositions { get; set; } = new Dictionary<string, double>();

        public double GetTraitPosition(Trait trait)
        {
            // Prefer stable Id key; migrate old localized keys if present
            if (TraitPositions.ContainsKey(trait.Id))
                return TraitPositions[trait.Id];

            if (!string.IsNullOrEmpty(trait.Name) && TraitPositions.ContainsKey(trait.Name))
            {
                var v = TraitPositions[trait.Name];
                // migrate to Id key and remove old localized key
                TraitPositions.Remove(trait.Name);
                TraitPositions[trait.Id] = v;
                return v;
            }

            // fallback default
            TraitPositions[trait.Id] = 0.5;
            return 0.5;
        }
    }
}
