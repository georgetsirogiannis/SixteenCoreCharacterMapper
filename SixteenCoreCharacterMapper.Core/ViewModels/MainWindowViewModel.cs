using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SixteenCoreCharacterMapper.Core.Models;
using SixteenCoreCharacterMapper.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace SixteenCoreCharacterMapper.Core.ViewModels
{
    public class LanguageOption
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public override string ToString() => Name;
    }

    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly IDialogService _dialogService;
        private readonly IWindowService _windowService;
        private readonly LocalizationService _localizationService;
        private readonly SettingsService _settingsService;

        [ObservableProperty]
        private Project _project;

        [ObservableProperty]
        private string? _currentFilePath;

        [ObservableProperty]
        private bool _isDirty;

        [ObservableProperty]
        private bool _isDarkMode = true;

        [ObservableProperty]
        private Character? _selectedCharacter;

        [ObservableProperty]
        private string _windowTitle = "16Core Character Mapper";

        [ObservableProperty]
        private LanguageOption? _selectedLanguage;

        [ObservableProperty]
        private bool _hasNotes;

        [ObservableProperty]
        private bool _hasCharacters;

        public event Action? RedrawTraitsRequested;
        public event Action? ApplyThemeRequested;
        public event Action<Character>? CharacterAdded;
        public event Action? RefreshCharacterListRequested;
        public event Action? CloseRequested;

        public ObservableCollection<LanguageOption> AvailableLanguages { get; }

        public MainWindowViewModel(IDialogService dialogService, IWindowService windowService)
        {
            _dialogService = dialogService;
            _windowService = windowService;
            _localizationService = new LocalizationService();
            _settingsService = new SettingsService();

            _project = new Project();
            _isDirty = false;
            UpdateWindowTitle();
            UpdateHasCharacters();

            AvailableLanguages = new ObservableCollection<LanguageOption>
            {
                new LanguageOption { Name = "English", Code = "en" },
                new LanguageOption { Name = "Français", Code = "fr" },
                new LanguageOption { Name = "Deutsch", Code = "de" },
                new LanguageOption { Name = "Español", Code = "es" },
                new LanguageOption { Name = "Português", Code = "pt" },
                new LanguageOption { Name = "Italiano", Code = "it" },
                new LanguageOption { Name = "Nederlands", Code = "nl" },
                new LanguageOption { Name = "Polski", Code = "pl" },
                new LanguageOption { Name = "Ελληνικά", Code = "el" },
            };

            var settings = _settingsService.LoadSettings();
            IsDarkMode = settings.IsDarkMode;

            var savedLanguage = AvailableLanguages.FirstOrDefault(l => l.Code == settings.LanguageCode);
            SelectedLanguage = savedLanguage ?? AvailableLanguages.First();
        }

        partial void OnSelectedLanguageChanged(LanguageOption? value)
        {
            if (value != null)
            {
                ChangeLanguage(value.Code);
                SaveSettings();
            }
        }

        partial void OnProjectChanged(Project value)
        {
            OnPropertyChanged(nameof(ProjectName));
            UpdateHasCharacters();
        }

        partial void OnIsDarkModeChanged(bool value)
        {
            ApplyThemeRequested?.Invoke();
            SaveSettings();
        }

        private void SaveSettings()
        {
            var settings = new AppSettings
            {
                IsDarkMode = IsDarkMode,
                LanguageCode = SelectedLanguage?.Code ?? "en"
            };
            _settingsService.SaveSettings(settings);
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
                else
                {
                    SetDirty(true);
                }
            }
        }

        public string MenuDashboard => _localizationService.GetString("MenuDashboard");
        public string MenuProducts => _localizationService.GetString("MenuProducts");

        [RelayCommand]
        private void ChangeLanguage(string languageCode)
        {
            _localizationService.SetCulture(languageCode);
            OnPropertyChanged(string.Empty);
            RedrawTraitsRequested?.Invoke();

            var match = AvailableLanguages.FirstOrDefault(l => l.Code == languageCode);
            if (match != null && !Equals(SelectedLanguage, match))
                SelectedLanguage = match;
        }

        public void SetDirty(bool isDirty)
        {
            IsDirty = isDirty;
            UpdateWindowTitle();
            UpdateHasNotes();
        }

        public void UpdateHasNotes()
        {
            HasNotes = Project?.TraitNotes != null && Project.TraitNotes.Any(n => !string.IsNullOrWhiteSpace(n.Value));
        }

        public void UpdateHasCharacters()
        {
            HasCharacters = Project?.Characters?.Count > 0;
        }

        private void UpdateWindowTitle()
        {
            var title = "16Core Character Mapper";
            var filePath = CurrentFilePath is null ? "New Project" : System.IO.Path.GetFileName(CurrentFilePath);
            var dirtyMarker = IsDirty ? "*" : "";
            WindowTitle = $"{title} - {filePath}{dirtyMarker}";
        }

        [RelayCommand]
        private async Task NewProject()
        {
            if (await ConfirmUnsavedChangesAsync())
            {
                Project = new Project();
                CurrentFilePath = null;
                SetDirty(false);
                UpdateHasNotes();
                UpdateHasCharacters();
                RedrawTraitsRequested?.Invoke();
                RefreshCharacterListRequested?.Invoke();
            }
        }

        [RelayCommand]
        private async Task LoadProject()
        {
            if (await ConfirmUnsavedChangesAsync())
            {
                var fileName = await _dialogService.OpenFileAsync("Character Map (*.16core;*.json)|*.16core;*.json");
                if (fileName != null)
                {
                    try
                    {
                        Project = ProjectService.Load(fileName);
                        CurrentFilePath = fileName;
                        SetDirty(false);
                        UpdateHasNotes();
                        UpdateHasCharacters();
                        OnPropertyChanged(nameof(ProjectName));
                        RedrawTraitsRequested?.Invoke();
                        RefreshCharacterListRequested?.Invoke();

                        if (!string.IsNullOrEmpty(Project.SelectedLanguage))
                        {
                            _localizationService.SetCulture(Project.SelectedLanguage);
                            SelectedLanguage = AvailableLanguages.FirstOrDefault(lang => lang.Code == Project.SelectedLanguage);
                        }
                    }
                    catch (Exception ex)
                    {
                        _dialogService.ShowMessage($"Error loading project: {ex.Message}", "Load Error");
                    }
                }
            }
        }

        [RelayCommand]
        private async Task SaveProject()
        {
            if (CurrentFilePath is null)
            {
                await SaveAsProject();
            }
            else
            {
                try
                {
                    if (SelectedLanguage != null)
                    {
                        Project.SelectedLanguage = SelectedLanguage.Code;
                    }

                    ProjectService.Save(Project, CurrentFilePath);
                    SetDirty(false);
                }
                catch (Exception ex)
                {
                    _dialogService.ShowMessage($"Error saving project: {ex.Message}", "Save Error");
                }
            }
        }

        [RelayCommand]
        private async Task SaveAsProject()
        {
            var defaultName = string.IsNullOrWhiteSpace(Project.Name) ? "Untitled Project" : Project.Name;
            var fileName = await _dialogService.SaveFileAsync("Character Map (*.16core)|*.16core|Legacy Character Map (*.json)|*.json", defaultName);

            if (fileName != null)
            {
                CurrentFilePath = fileName;
                await SaveProject();
            }
        }

        [RelayCommand]
        private async Task AddCharacter()
        {
            var newCharacter = await _windowService.ShowEditCharacterDialogAsync(null, IsDarkMode);
            if (newCharacter != null)
            {
                if (string.IsNullOrWhiteSpace(newCharacter.Name))
                {
                    var baseName = "Character";
                    int n = 1;
                    var names = new HashSet<string>(Project.Characters.Select(c => c.Name), StringComparer.OrdinalIgnoreCase);
                    while (names.Contains($"{baseName} {n}")) n++;
                    newCharacter.Name = $"{baseName} {n}";
                }
                newCharacter.DisplayOrder = Project.Characters.Count;
                Project.Characters.Add(newCharacter);
                SetDirty(true);
                UpdateHasCharacters();
                RedrawTraitsRequested?.Invoke();
                RefreshCharacterListRequested?.Invoke();
                CharacterAdded?.Invoke(newCharacter);
            }
        }

        [RelayCommand]
        private async Task EditCharacter()
        {
            if (SelectedCharacter == null) return;
            var updatedCharacter = await _windowService.ShowEditCharacterDialogAsync(SelectedCharacter, IsDarkMode);
            if (updatedCharacter != null)
            {
                SetDirty(true);
                RedrawTraitsRequested?.Invoke();
                RefreshCharacterListRequested?.Invoke();
            }
        }

        [RelayCommand]
        private async Task DeleteCharacter()
        {
            if (SelectedCharacter == null) return;
            var result = await _dialogService.ConfirmAsync($"Are you sure you want to delete \"{SelectedCharacter.Name}\"?", "Confirm Deletion");
            if (result)
            {
                Project.Characters.Remove(SelectedCharacter);
                SetDirty(true);
                UpdateHasCharacters();
                RedrawTraitsRequested?.Invoke();
                RefreshCharacterListRequested?.Invoke();
            }
        }

        [RelayCommand]
        private void ToggleTheme()
        {
            IsDarkMode = !IsDarkMode;
        }

        [RelayCommand]
        private void ToggleLock(Character? character)
        {
            if (character != null)
            {
                character.IsLocked = !character.IsLocked;
                SetDirty(true);
                RedrawTraitsRequested?.Invoke();
            }
        }

        [RelayCommand]
        private void ToggleVisibility(Character? character)
        {
            if (character != null)
            {
                character.IsVisible = !character.IsVisible;
                SetDirty(true);
                RedrawTraitsRequested?.Invoke();
            }
        }

        private bool ConfirmUnsavedChanges()
        {
            if (!IsDirty) return true;

            var result = _dialogService.AskUnsavedChanges();
            if (result == DialogResult.Yes)
            {
                SaveProject().GetAwaiter().GetResult();
                return true;
            }
            return result == DialogResult.No;
        }

        private async Task<bool> ConfirmUnsavedChangesAsync()
        {
            if (!IsDirty) return true;

            var result = await _dialogService.AskUnsavedChangesAsync();
            if (result == DialogResult.Yes)
            {
                await SaveProject();
                return true;
            }
            return result == DialogResult.No;
        }

        private bool _isExiting = false;

        public void ClosingWindow(CancelEventArgs e)
        {
            if (_isExiting) return;

            if (IsDirty)
            {
                e.Cancel = true;
                _ = HandleUnsavedChangesOnExitAsync();
            }
        }

        private async Task HandleUnsavedChangesOnExitAsync()
        {
            if (await ConfirmUnsavedChangesAsync())
            {
                _isExiting = true;
                CloseRequested?.Invoke();
            }
        }
    }
}
