using System.Windows.Media;

namespace SixteenCoreCharacterMapper
{
    /// <summary>
    /// A helper class to store a color and its name for the preset palette.
    /// </summary>
    public class ColorItem
    {
        public Color Color { get; set; }
        public string Name { get; set; }

        public ColorItem(Color color, string name)
        {
            Color = color;
            Name = name;
        }
    }
}