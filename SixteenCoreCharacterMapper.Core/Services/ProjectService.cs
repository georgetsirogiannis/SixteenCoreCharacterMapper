using System;
using System.IO;
using System.Text.Json;
using System.Linq;
using SixteenCoreCharacterMapper.Core.Models;

namespace SixteenCoreCharacterMapper.Core.Services
{
    public static class ProjectService
    {
        public static void Save(Project project, string filePath)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(project, options);
            File.WriteAllText(filePath, json);
        }

        public static Project Load(string filePath)
        {
            var json = File.ReadAllText(filePath);
            var project = JsonSerializer.Deserialize<Project>(json);

            if (project == null)
            {
                throw new InvalidOperationException("Could not read the project file. It may be empty or corrupt.");
            }

            // Ensure the Characters collection is an ObservableCollection
            project.Characters = new System.Collections.ObjectModel.ObservableCollection<Character>(project.Characters.ToList());
            
            if (project.TraitNotes == null)
            {
                project.TraitNotes = new System.Collections.Generic.Dictionary<string, string>();
            }

            return project;
        }
    }
}
