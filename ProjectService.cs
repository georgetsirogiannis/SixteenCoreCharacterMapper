using System;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace SixteenCoreCharacterMapper
{
    public static class ProjectService
    {
        public static bool Save(Project project, string filePath)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(project, options);
                File.WriteAllText(filePath, json);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save project file: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public static Project Load(string filePath)
        {
            try
            {
                var json = File.ReadAllText(filePath);
                var project = JsonSerializer.Deserialize<Project>(json);

                if (project == null)
                {
                    MessageBox.Show("Could not read the project file. It may be empty or corrupt.", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return new Project();
                }

                // Ensure the Characters collection is an ObservableCollection
                project.Characters = new System.Collections.ObjectModel.ObservableCollection<Character>(project.Characters.ToList());
                return project;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load project file: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return new Project(); // Return a new project so the app doesn't crash
            }
        }
    }
}
