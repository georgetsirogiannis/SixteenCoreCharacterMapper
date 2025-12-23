namespace SixteenCoreCharacterMapper.Core.Models
{
    public class ColorItem
    {
        public string Hex { get; set; }
        public string Name { get; set; }

        public ColorItem(string hex, string name)
        {
            Hex = hex;
            Name = name;
        }
    }
}
