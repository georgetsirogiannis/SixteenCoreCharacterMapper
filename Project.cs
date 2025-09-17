using System.Collections.ObjectModel;

namespace SixteenCoreCharacterMapper
{
    public class Project
    {
        // Add a Name property to the Project class to store the project name.
        public string Name { get; set; } = string.Empty;
        public ObservableCollection<Character> Characters { get; set; } = new ObservableCollection<Character>();

        // Add a property to store the selected language.
        public string SelectedLanguage { get; set; } = "en"; // Default to English.
    }
}
