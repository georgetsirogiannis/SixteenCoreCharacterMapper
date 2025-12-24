using System;
using System.IO;
using System.Text.Json;
using System.Linq;
using System.Threading.Tasks;
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

        public static async Task SaveAsync(Project project, string filePath)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
            await JsonSerializer.SerializeAsync(stream, project, options);
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

        public static async Task<Project> LoadAsync(string filePath)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
            var project = await JsonSerializer.DeserializeAsync<Project>(stream);

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
