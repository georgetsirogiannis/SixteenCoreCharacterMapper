using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace SixteenCoreCharacterMapper.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private Project _project;
        private string? _currentFilePath;
        private bool _isDirty;
        private bool _isDarkMode = true;
        private Character? _selectedCharacter;
        private string _windowTitle = "16Core Character Mapper";

        public event Action? RedrawTraitsRequested;
        public event Action? ApplyThemeRequested;
        public event Action<Character>? CharacterAdded;
        public event Action? RefreshCharacterListRequested;


        public Project Project
        {
            get => _project;
            set
            {
                if (SetProperty(ref _project, value))
                {
                    OnPropertyChanged(nameof(ProjectName));
                }
            }
        }

        public string ProjectName
        {
            get => Project.Name;
            set
            {
                if (Project.Name != value)
                {
                    Project.Name = value;
                    OnPropertyChanged();
                    SetDirty(true);
                }
            }
        }

        public Character? SelectedCharacter
        {
            get => _selectedCharacter;
            set => SetProperty(ref _selectedCharacter, value);
        }

        public string WindowTitle
        {
            get => _windowTitle;
            set => SetProperty(ref _windowTitle, value);
        }

        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                if (SetProperty(ref _isDarkMode, value))
                {
                    ApplyThemeRequested?.Invoke();
                }
            }
        }

        public ICommand NewProjectCommand { get; }
        public ICommand LoadProjectCommand { get; }
        public ICommand SaveProjectCommand { get; }
        public ICommand SaveAsProjectCommand { get; }
        public ICommand AddCharacterCommand { get; }
        public ICommand EditCharacterCommand { get; }
        public ICommand DeleteCharacterCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand ToggleLockCommand { get; }
        public ICommand ToggleVisibilityCommand { get; }

        public MainWindowViewModel()
        {
            _project = new Project();
            _isDirty = false;
            UpdateWindowTitle();

            NewProjectCommand = new RelayCommand(NewProject);
            LoadProjectCommand = new RelayCommand(LoadProject);
            SaveProjectCommand = new RelayCommand(() => { SaveProject(); });
            SaveAsProjectCommand = new RelayCommand(() => { SaveAsProject(); });
            AddCharacterCommand = new RelayCommand(AddCharacter);
            EditCharacterCommand = new RelayCommand(EditCharacter, () => SelectedCharacter != null);
            DeleteCharacterCommand = new RelayCommand(DeleteCharacter, () => SelectedCharacter != null);
            ToggleThemeCommand = new RelayCommand(() => IsDarkMode = !IsDarkMode);
            ToggleLockCommand = new RelayCommand<Character>(ToggleLock);
            ToggleVisibilityCommand = new RelayCommand<Character>(ToggleVisibility);
        }

        public void SetDirty(bool isDirty)
        {
            _isDirty = isDirty;
            UpdateWindowTitle();
        }

        private void UpdateWindowTitle()
        {
            var title = "16Core Character Mapper";
            var filePath = _currentFilePath is null ? "New Project" : System.IO.Path.GetFileName(_currentFilePath);
            var dirtyMarker = _isDirty ? "*" : "";
            WindowTitle = $"{title} - {filePath}{dirtyMarker}";
        }

        private void NewProject()
        {
            if (ConfirmUnsavedChanges())
            {
                Project = new Project();
                _currentFilePath = null;
                SetDirty(false);
                RedrawTraitsRequested?.Invoke();
            }
        }

        private void LoadProject()
        {
            if (ConfirmUnsavedChanges())
            {
                var dlg = new OpenFileDialog { Filter = "Character Map (*.json)|*.json" };
                if (dlg.ShowDialog() == true)
                {
                    try
                    {
                        Project = ProjectService.Load(dlg.FileName);
                        _currentFilePath = dlg.FileName;
                        SetDirty(false);
                        RedrawTraitsRequested?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading project: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private bool SaveProject()
        {
            if (_currentFilePath is null)
            {
                return SaveAsProject();
            }
            else
            {
                try
                {
                    ProjectService.Save(Project, _currentFilePath);
                    SetDirty(false);
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving project: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
        }

        private bool SaveAsProject()
        {
            var dlg = new SaveFileDialog
            {
                Filter = "Character Map (*.json)|*.json",
                FileName = string.IsNullOrWhiteSpace(Project.Name) ? "Untitled Project" : Project.Name
            };

            if (dlg.ShowDialog() == true)
            {
                _currentFilePath = dlg.FileName;
                Project.Name = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName);
                OnPropertyChanged(nameof(ProjectName));
                return SaveProject();
            }
            return false;
        }

        private void AddCharacter()
        {
            var dlg = new EditCharacterDialog(IsDarkMode) { Owner = Application.Current!.MainWindow };
            if (dlg.ShowDialog() == true)
            {
                // This new 'if' block checks that the character is not null before proceeding.
                if (dlg.Character != null)
                {
                    if (string.IsNullOrWhiteSpace(dlg.Character.Name))
                    {
                        var baseName = "Character";
                        int n = 1;
                        var names = new HashSet<string>(_project.Characters.Select(c => c.Name), StringComparer.OrdinalIgnoreCase);
                        while (names.Contains($"{baseName} {n}")) n++;
                        dlg.Character.Name = $"{baseName} {n}";
                    }
                    dlg.Character.DisplayOrder = _project.Characters.Count; // Assign initial order
                    _project.Characters.Add(dlg.Character);
                    SetDirty(true);
                    RedrawTraitsRequested?.Invoke();
                    CharacterAdded?.Invoke(dlg.Character);
                }
            }
        }

        private void EditCharacter()
        {
            if (SelectedCharacter == null) return;
            var dlg = new EditCharacterDialog(SelectedCharacter, IsDarkMode) { Owner = Application.Current!.MainWindow };
            if (dlg.ShowDialog() == true)
            {
                SetDirty(true);
                RedrawTraitsRequested?.Invoke();
                RefreshCharacterListRequested?.Invoke();
            }
        }

        private void DeleteCharacter()
        {
            if (SelectedCharacter == null) return;
            var result = MessageBox.Show(
                $"Are you sure you want to delete \"{SelectedCharacter.Name}\"?",
                "Confirm Deletion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                Project.Characters.Remove(SelectedCharacter);
                SetDirty(true);
                RedrawTraitsRequested?.Invoke();
            }
        }

        private void ToggleLock(Character? character)
        {
            if (character != null)
            {
                character.IsLocked = !character.IsLocked;
                SetDirty(true);
                RedrawTraitsRequested?.Invoke();
                RefreshCharacterListRequested?.Invoke();
            }
        }

        private void ToggleVisibility(Character? character)
        {
            if (character != null)
            {
                character.IsVisible = !character.IsVisible;
                SetDirty(true);
                RedrawTraitsRequested?.Invoke();
                RefreshCharacterListRequested?.Invoke();
            }
        }

        private bool ConfirmUnsavedChanges()
        {
            if (!_isDirty) return true;

            var result = MessageBox.Show(
                "You have unsaved changes. Would you like to save them?",
                "Unsaved Changes",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                return SaveProject();
            }
            return result == MessageBoxResult.No;
        }

        public void ClosingWindow(CancelEventArgs e)
        {
            if (!ConfirmUnsavedChanges())
            {
                e.Cancel = true;
            }
        }
    }
}
