using System.Collections.ObjectModel;

namespace SixteenCoreCharacterMapper.Core.Models
{
    public class Project
    {
        public string Name { get; set; } = string.Empty;
        public ObservableCollection<Character> Characters { get; set; } = new ObservableCollection<Character>();
        public string SelectedLanguage { get; set; } = "en";
    }
}
