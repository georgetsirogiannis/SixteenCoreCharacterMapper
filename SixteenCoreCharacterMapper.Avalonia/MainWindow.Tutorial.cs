using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using SixteenCoreCharacterMapper.Core.Models;
using System;
using System.Linq;

namespace SixteenCoreCharacterMapper.Avalonia
{
    public partial class MainWindow
    {
        private bool _isTutorialActive = false;
        private TutorialStage _currentTutorialStage = TutorialStage.None;
        private bool _tutorialLockToggled = false;
        private bool _tutorialUnlockToggled = false;
        private bool _tutorialHideToggled = false;
        private bool _tutorialShowToggled = false;

        private enum TutorialStage
        {
            None, ProjectName, AddCharacter, DragBubble, LockCharacter,
            ShowHideCharacter, ToggleTheme, ExportImage, Final
        }

        private void Tutorial_Click(object? sender, RoutedEventArgs e) { if (_isTutorialActive) StopTutorial(); else StartTutorial(); }
        private void StartTutorial() { _isTutorialActive = true; _currentTutorialStage = TutorialStage.None; ProceedToNextTutorialStage(); }
        private void StopTutorial() 
        { 
            _isTutorialActive = false; 
            _currentTutorialStage = TutorialStage.None; 
            
            SetHighlight("ProjectNameHighlight", false);
            SetHighlight("CharacterButtonsHighlight", false);
            SetHighlight("ToggleThemeHighlight", false);
            SetHighlight("ExportImageHighlight", false);
            
            var popup = this.FindControl<Popup>("TutorialPopup");
            if (popup != null) popup.IsOpen = false;

            // Hide list item highlights
            // We need to iterate all 3 lists now
            HideHighlightsInList(this.FindControl<ListBox>("MainList"));
            HideHighlightsInList(this.FindControl<ListBox>("SupportingList"));
            HideHighlightsInList(this.FindControl<ListBox>("BackgroundList"));

            RedrawBubblesInTraits(); 
        }

        private void HideHighlightsInList(ListBox? list)
        {
             if (list == null) return;
             foreach (var item in list.GetRealizedContainers())
             {
                 var borders = item.GetVisualDescendants().OfType<Border>();
                 foreach (var b in borders)
                 {
                     if (b.Name == "LockHighlight" || b.Name == "VisibilityHighlight")
                         b.IsVisible = false;
                 }
             }
        }

        private void ProceedToNextTutorialStage() 
        { 
            if (!_isTutorialActive) return; 
            
            SetHighlight("ProjectNameHighlight", false);
            SetHighlight("CharacterButtonsHighlight", false);
            SetHighlight("ToggleThemeHighlight", false);
            SetHighlight("ExportImageHighlight", false);
            
            HideHighlightsInList(this.FindControl<ListBox>("MainList"));
            HideHighlightsInList(this.FindControl<ListBox>("SupportingList"));
            HideHighlightsInList(this.FindControl<ListBox>("BackgroundList"));

            RedrawBubblesInTraits(); 
            _currentTutorialStage++; 
            ShowTutorialStage(_currentTutorialStage); 
        }

        private void ShowTutorialStage(TutorialStage stage)
        {
            var popup = this.FindControl<Popup>("TutorialPopup");
            var textBlock = this.FindControl<TextBlock>("TutorialTextBlock");
            var button = this.FindControl<Button>("TutorialButton");
            
            if (popup == null || textBlock == null || button == null) return;

            popup.IsOpen = false;
            button.IsVisible = false;

            Control? target = null;
            PlacementMode placement = PlacementMode.Bottom;

            switch (stage)
            {
                case TutorialStage.ProjectName: 
                    textBlock.Text = "Enter your project title here."; 
                    target = this.FindControl<Control>("ProjectNameTextBox"); 
                    SetHighlight("ProjectNameHighlight", true); 
                    break;
                case TutorialStage.AddCharacter: 
                    textBlock.Text = _viewModel?.Project.Characters.Any() == true ? "Add another character here!\n\nOn the window that opens, enter a name and pick a color.\nThen select your character type, on which depends the size of the character's bubble on the chart.\nFinally, click OK." : "Add your first character here!\n\nOn the window that opens, enter a name and pick a color.\nThen select your character type, on which depends the size of the character's bubble on the chart.\nFinally, click OK."; 
                    target = this.FindControl<Control>("AddCharacterButton"); 
                    SetHighlight("CharacterButtonsHighlight", true); 
                    break;
                case TutorialStage.DragBubble: 
                    if (_viewModel?.Project.Characters.Any() != true) { StopTutorial(); return; } 
                    textBlock.Text = "Drag your character's bubble on the 16 trait lines."; 
                    // Find first bubble
                    // We need to wait for layout?
                    // Just target TraitsGrid for now or find bubble
                    target = _traitsGrid;
                    placement = PlacementMode.Center;
                    popup.IsOpen = true; 
                    RedrawBubblesInTraits(); 
                    return;
                case TutorialStage.LockCharacter: 
                    _tutorialLockToggled = false; _tutorialUnlockToggled = false; 
                    textBlock.Text = "You can lock and unlock the position of your character's bubbles here.\n\nTry it now!"; 
                    
                    target = FindButtonInLists("LockButton", "LockHighlight");
                    if (target != null) placement = PlacementMode.Right;
                    else StopTutorial();
                    break;

                case TutorialStage.ShowHideCharacter: 
                    _tutorialHideToggled = false; _tutorialShowToggled = false; 
                    textBlock.Text = "You can hide and show your character's bubbles here.\n\nTry it now!"; 
                    
                    target = FindButtonInLists("VisibilityButton", "VisibilityHighlight");
                    if (target != null) placement = PlacementMode.Right;
                    else StopTutorial();
                    break;
                case TutorialStage.ToggleTheme: 
                    textBlock.Text = "You can toggle between Light and Dark Mode here.\n\nTry it now!"; 
                    target = this.FindControl<Control>("ToggleThemeButton"); 
                    SetHighlight("ToggleThemeHighlight", true); 
                    break;
                case TutorialStage.ExportImage: 
                    SetHighlight("ToggleThemeHighlight", false); 
                    textBlock.Text = "You can export your character map to a PNG image here."; 
                    target = this.FindControl<Control>("ExportImageButton"); 
                    SetHighlight("ExportImageHighlight", true); 
                    button.IsVisible = true; 
                    button.Content = "Got it!"; 
                    break;
                case TutorialStage.Final: 
                    SetHighlight("ExportImageHighlight", false); 
                    textBlock.Text = "Add more characters and drag their bubbles to map their similarities and differences.\n\nThat's all!"; 
                    target = this.FindControl<Control>("MainList"); // Point to list
                    placement = PlacementMode.Right; 
                    button.IsVisible = true; 
                    button.Content = "Finish tutorial"; 
                    break;
                default: StopTutorial(); break;
            }

            if (target != null)
            {
                popup.PlacementTarget = target;
                popup.Placement = placement;
                popup.IsOpen = true;
            }
        }

        private Control? FindButtonInLists(string buttonName, string highlightName)
        {
            var lists = new[] { "MainList", "SupportingList", "BackgroundList" };
            foreach (var listName in lists)
            {
                var list = this.FindControl<ListBox>(listName);
                if (list != null && list.ItemCount > 0)
                {
                    var container = list.ContainerFromIndex(0);
                    if (container != null)
                    {
                        var btn = container.GetVisualDescendants().OfType<Button>().FirstOrDefault(b => b.Name == buttonName);
                        var highlight = container.GetVisualDescendants().OfType<Border>().FirstOrDefault(b => b.Name == highlightName);
                        
                        if (highlight != null) highlight.IsVisible = true;
                        if (btn != null) return btn;
                    }
                }
            }
            return null;
        }

        private void TutorialButton_Click(object? sender, RoutedEventArgs e) { if (_currentTutorialStage == TutorialStage.ExportImage) ProceedToNextTutorialStage(); else if (_currentTutorialStage == TutorialStage.Final) StopTutorial(); }

        private void SetHighlight(string name, bool visible)
        {
            var border = this.FindControl<Border>(name);
            if (border != null) border.IsVisible = visible;
        }

        private void OnCharacterAddedForTutorial(Character newCharacter)
        {
            if (_isTutorialActive && _currentTutorialStage == TutorialStage.AddCharacter)
            {
                ProceedToNextTutorialStage();
            }
        }

        private void ProjectNameTextBox_TextChanged(object? sender, TextChangedEventArgs e)
        {
            if (_isTutorialActive && _currentTutorialStage == TutorialStage.ProjectName)
            {
                var textBox = sender as TextBox;
                if (!string.IsNullOrWhiteSpace(textBox?.Text))
                {
                    ProceedToNextTutorialStage();
                }
            }
        }

        private void ToggleThemeButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_isTutorialActive && _currentTutorialStage == TutorialStage.ToggleTheme)
            {
                ProceedToNextTutorialStage();
            }
        }

        private void IconButton_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Character character)
            {
                if (button.Name == "LockButton")
                {
                    if (_isTutorialActive && _currentTutorialStage == TutorialStage.LockCharacter)
                    {
                        if (!character.IsLocked) _tutorialLockToggled = true;
                        else _tutorialUnlockToggled = true;

                        if (_tutorialLockToggled && _tutorialUnlockToggled) ProceedToNextTutorialStage();
                    }
                }
                else if (button.Name == "VisibilityButton")
                {
                    if (_isTutorialActive && _currentTutorialStage == TutorialStage.ShowHideCharacter)
                    {
                        if (character.IsVisible) _tutorialHideToggled = true;
                        else _tutorialShowToggled = true;

                        if (_tutorialHideToggled && _tutorialShowToggled) ProceedToNextTutorialStage();
                    }
                }
            }
        }
    }
}
