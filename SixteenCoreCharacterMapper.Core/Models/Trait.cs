namespace SixteenCoreCharacterMapper.Core.Models
{
    public class Trait
    {
        // Stable identifier used as dictionary key for positions (does not change with localization)
        public string Id { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public string LowLabel { get; set; }
        public string HighLabel { get; set; }

        public Trait(string id, string name, string description, string lowLabel, string highLabel)
        {
            Id = id;
            Name = name;
            Description = description;
            LowLabel = lowLabel;
            HighLabel = highLabel;
        }
    }
}
