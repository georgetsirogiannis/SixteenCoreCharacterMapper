namespace SixteenCoreCharacterMapper
{
    public class Trait
    {
        public string Name { get; set; }
        public string Description { get; set; } // New property for the description
        public string LowLabel { get; set; }
        public string HighLabel { get; set; }

        // Updated constructor to include the description
        public Trait(string name, string description, string lowLabel, string highLabel)
        {
            Name = name;
            Description = description;
            LowLabel = lowLabel;
            HighLabel = highLabel;
        }
    }
}
