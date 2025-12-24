using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using SixteenCoreCharacterMapper.Core;
using SixteenCoreCharacterMapper.Core.Models;
using SixteenCoreCharacterMapper.Core.Services;
using SixteenCoreCharacterMapper.Core.ViewModels;
using SixteenCoreCharacterMapper.Avalonia.Services;
using System.Linq;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Controls.Shapes;
using System.Collections.Generic;
using System;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using System.IO;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using Avalonia.Controls.Primitives;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia.Styling;

namespace SixteenCoreCharacterMapper.Avalonia
{
    public partial class MainWindow : Window
    {
        private MainWindowViewModel? _viewModel;
        private Grid? _traitsGrid;
        private readonly IUpdateService _updateService = new UpdateService();
        private readonly IImageExportService _imageExportService = new AvaloniaImageExportService();
        
        public ObservableCollection<Character> MainCharacters { get; } = new();
        public ObservableCollection<Character> SupportingCharacters { get; } = new();
        public ObservableCollection<Character> BackgroundCharacters { get; } = new();

        private Trait? _draggingTrait;
        private Ellipse? _draggingEllipse;
        private double _dragStartOffsetX;
        private Point _dragStartPoint;
        private bool _isPotentialDrag; // New field
        private ListBoxItem? _currentDropTarget;
        private Trait? _currentNoteTrait;

        public MainWindow()
        {
            InitializeComponent();
            this.SizeChanged += Window_SizeChanged;
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);

            if (Screens.Primary is { } primaryScreen)
            {
                if (primaryScreen.Bounds.Width >= 1920 && primaryScreen.Bounds.Height >= 1080)
                {
                    Width = 1920;
                    Height = 960;
                    WindowState = WindowState.Normal;

                    // Center the window
                    var x = primaryScreen.Bounds.X + (primaryScreen.Bounds.Width - 1920) / 2;
                    var y = primaryScreen.Bounds.Y + (primaryScreen.Bounds.Height - 960) / 2;
                    Position = new PixelPoint((int)x, (int)y);
                }
                else
                {
                    WindowState = WindowState.Maximized;
                }
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            _traitsGrid = this.FindControl<Grid>("TraitsGrid");
        }

        private void Window_SizeChanged(object? sender, SizeChangedEventArgs e)
        {
            const double designWidth = 1800;
            const double designHeight = 900;

            double newWidth = e.NewSize.Width;
            double newHeight = e.NewSize.Height;

            if (newWidth <= 0 || newHeight <= 0) return;

            bool isScalingNeeded = newWidth < designWidth || newHeight < designHeight;
            
            var rootLayoutControl = this.FindControl<LayoutTransformControl>("RootLayoutControl");
            if (rootLayoutControl == null) return;

            if (isScalingNeeded)
            {
                double scaleX = newWidth / designWidth;
                double scaleY = newHeight / designHeight;
                double scale = Math.Min(scaleX, scaleY);
                
                rootLayoutControl.LayoutTransform = new ScaleTransform(scale, scale);
            }
            else
            {
                rootLayoutControl.LayoutTransform = new ScaleTransform(1, 1);
            }
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            if (DataContext is MainWindowViewModel vm)
            {
                _viewModel = vm;
                _viewModel.RebuildTraitsRequested += InitializeTraits;
                _viewModel.RefreshBubblesRequested += RedrawBubblesInTraits;
                _viewModel.ApplyThemeRequested += () => ApplyTheme(_viewModel.IsDarkMode);
                _viewModel.CharacterAdded += OnCharacterAddedForTutorial;
                _viewModel.RefreshCharacterListRequested += UpdateGroups;
                _viewModel.CloseRequested += Close;

                InitializeTraits();
                ApplyTheme(_viewModel.IsDarkMode);
                UpdateGroups();
                
                // Check for updates on startup
                _ = CheckForUpdatesWithResultAsync();
            }
        }

        private void UpdateGroups()
        {
            if (_viewModel?.Project == null) return;

            var selected = _viewModel.SelectedCharacter;

            MainCharacters.Clear();
            SupportingCharacters.Clear();
            BackgroundCharacters.Clear();

            var sorted = _viewModel.Project.Characters.OrderBy(c => c.DisplayOrder);
            foreach (var c in sorted)
            {
                switch (c.Size)
                {
                    case BubbleSize.Large: MainCharacters.Add(c); break;
                    case BubbleSize.Medium: SupportingCharacters.Add(c); break;
                    case BubbleSize.Small: BackgroundCharacters.Add(c); break;
                }
            }

            if (selected != null)
            {
                // Restore selection if it was lost during Clear
                if (_viewModel.SelectedCharacter != selected)
                    _viewModel.SelectedCharacter = selected;
            }

            UpdateSectionVisibility();
        }

        private void UpdateSectionVisibility()
        {
            var mainHeader = this.FindControl<TextBlock>("MainHeader");
            var mainList = this.FindControl<ListBox>("MainList");
            if (mainHeader != null) mainHeader.IsVisible = MainCharacters.Count > 0;
            if (mainList != null) mainList.IsVisible = MainCharacters.Count > 0;

            var supportingHeader = this.FindControl<TextBlock>("SupportingHeader");
            var supportingList = this.FindControl<ListBox>("SupportingList");
            if (supportingHeader != null) supportingHeader.IsVisible = SupportingCharacters.Count > 0;
            if (supportingList != null) supportingList.IsVisible = SupportingCharacters.Count > 0;

            var backgroundHeader = this.FindControl<TextBlock>("BackgroundHeader");
            var backgroundList = this.FindControl<ListBox>("BackgroundList");
            if (backgroundHeader != null) backgroundHeader.IsVisible = BackgroundCharacters.Count > 0;
            if (backgroundList != null) backgroundList.IsVisible = BackgroundCharacters.Count > 0;
        }

        private void InitializeTraits()
        {
            if (_traitsGrid == null) return;

            _traitsGrid.Children.Clear();
            _traitsGrid.RowDefinitions.Clear();
            _traitsGrid.ColumnDefinitions.Clear();

            int traitIndex = 0;
            const int columnsPerRow = 4;
            const int totalTraits = 16;
            int totalRows = totalTraits / columnsPerRow;

            for (int i = 0; i < totalRows; i++)
            {
                _traitsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            }
            for (int i = 0; i < columnsPerRow; i++)
            {
                _traitsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            foreach (var trait in TraitDefinitions.All)
            {
                int rowIndex = traitIndex / columnsPerRow;
                int columnIndex = traitIndex % columnsPerRow;

                var containerBorder = new Border { Padding = new Thickness(15, 22, 15, 10), Tag = trait };
                var innerGrid = new Grid();
                innerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                innerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) });
                innerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                // Title Grid
                var titleGrid = new Grid();
                titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.4, GridUnitType.Star) });
                titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.6, GridUnitType.Star) });
                titleGrid.Margin = new Thickness(0, 0, 0, 12);
                titleGrid.MinHeight = 15 * 2 * 1.25;

                var title = new TextBlock
                {
                    Text = trait.Name,
                    FontWeight = FontWeight.Bold,
                    FontSize = 15,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    TextWrapping = TextWrapping.Wrap,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    LetterSpacing = -0.1,
                    LineHeight = 15 * 1.25,
                    Classes = { "TraitTitle" }
                };
                Grid.SetColumn(title, 0);
                titleGrid.Children.Add(title);

                var description = new TextBlock 
                { 
                    Text = trait.Description, 
                    FontSize = 9, 
                    HorizontalAlignment = HorizontalAlignment.Right, 
                    VerticalAlignment = VerticalAlignment.Bottom, 
                    TextAlignment = TextAlignment.Right,
                    LetterSpacing = -0.2,
                    TextWrapping = TextWrapping.Wrap,
                    FontWeight = FontWeight.Medium,
                    Classes = { "TraitDescription" }
                };
                Grid.SetColumn(description, 1);
                titleGrid.Children.Add(description);

                var noteContainer = new Grid
                {
                    Width = 20,
                    Height = 20,
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, -10, 0, 0)
                };

                var noteButton = new Button
                {
                    Classes = { "IconButton" },
                    Width = 20,
                    Height = 20,
                    Tag = trait
                };
                ToolTip.SetTip(noteButton, "Notes");
                noteButton.Click += NoteButton_Click;

                bool hasNote = _viewModel?.Project?.TraitNotes != null && 
                               _viewModel.Project.TraitNotes.TryGetValue(trait.Id, out var note) && 
                               !string.IsNullOrWhiteSpace(note);
                noteButton.Opacity = hasNote ? 1.0 : 0.3;

                var noteIcon = new PathIcon
                {
                    Data = StreamGeometry.Parse("M14,2H6A2,2 0 0,0 4,4V20A2,2 0 0,0 6,22H18A2,2 0 0,0 20,20V8L14,2M18,20H6V4H13V9H18V20M8,12H16V14H8V12M8,16H16V18H8V16Z"),
                    Width = 12,
                    Height = 12
                };
                noteButton.Content = noteIcon;
                
                var noteHighlight = new Border
                {
                    BorderBrush = Brush.Parse("#ad87ff"),
                    BorderThickness = new Thickness(2),
                    IsVisible = false,
                    IsHitTestVisible = false,
                    CornerRadius = new CornerRadius(3),
                    Tag = "NoteHighlight"
                };

                noteContainer.Children.Add(noteButton);
                noteContainer.Children.Add(noteHighlight);

                Grid.SetColumn(noteContainer, 1);
                titleGrid.Children.Add(noteContainer);
                
                innerGrid.Children.Add(titleGrid);
                Grid.SetRow(titleGrid, 0);

                // Canvas
                var canvas = new Canvas { Background = Brushes.Transparent, Tag = trait };
                canvas.PointerPressed += Canvas_PointerPressed;
                canvas.PointerMoved += Canvas_PointerMoved;
                canvas.PointerReleased += Canvas_PointerReleased;

                Grid.SetRow(canvas, 1);
                
                var baseline = new Line
                {
                    StartPoint = new Point(0, 25),
                    EndPoint = new Point(300, 25), 
                    Stroke = Brushes.Gray,
                    StrokeThickness = 2,
                    Classes = { "TraitLine" }
                };
                
                canvas.SizeChanged += (s, e) => 
                {
                    baseline.EndPoint = new Point(canvas.Bounds.Width, 25);
                    RedrawBubbles(canvas);
                };

                canvas.Children.Add(baseline);
                innerGrid.Children.Add(canvas);

                // Labels
                var labelsGrid = new Grid();
                labelsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.33, GridUnitType.Star) });
                labelsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.34, GridUnitType.Star) });
                labelsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.33, GridUnitType.Star) });
                labelsGrid.Margin = new Thickness(0, 5, 0, 0);
                
                var leftLabel = new TextBlock { Text = trait.LowLabel, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, FontSize = 9, FontStyle = FontStyle.Italic, FontWeight = FontWeight.Medium, LetterSpacing = -0.2, TextWrapping = TextWrapping.Wrap, Classes = { "TraitLabel" }, Padding = new Thickness(2, 0, 0, 0) }
            ;
                Grid.SetColumn(leftLabel, 0);
                
                var rightLabel = new TextBlock { Text = trait.HighLabel, TextAlignment = TextAlignment.Right, VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Right, FontSize = 9, FontStyle = FontStyle.Italic, FontWeight = FontWeight.Medium, LetterSpacing = -0.2, TextWrapping = TextWrapping.Wrap, Classes = { "TraitLabel" }, Padding = new Thickness(0, 0, 2, 0) };
                Grid.SetColumn(rightLabel, 2);
                
                labelsGrid.Children.Add(leftLabel);
                labelsGrid.Children.Add(rightLabel);
                
                innerGrid.Children.Add(labelsGrid);
                Grid.SetRow(labelsGrid, 2);

                containerBorder.Child = innerGrid;
                Grid.SetRow(containerBorder, rowIndex);
                Grid.SetColumn(containerBorder, columnIndex);
                _traitsGrid.Children.Add(containerBorder);
                traitIndex++;
            }
            
            ApplyTheme(_viewModel?.IsDarkMode ?? true);
        }

        private void RedrawBubbles(Canvas canvas)
        {
            if (_viewModel?.Project == null || canvas.Tag is not Trait trait) return;

            var toRemove = canvas.Children.OfType<Ellipse>().ToList();
            foreach (var item in toRemove) canvas.Children.Remove(item);

            foreach (var ch in _viewModel.Project.Characters)
            {
                if (!ch.IsVisible) continue;

                double pos = ch.GetTraitPosition(trait);

                double size = ch.Size switch { BubbleSize.Large => 40, BubbleSize.Medium => 25, _ => 15 };
                double usable = System.Math.Max(1, canvas.Bounds.Width);
                double x = pos * (usable - size);
                double y = (50 - size) / 2;

                var ellipse = new Ellipse
                {
                    Width = size,
                    Height = size,
                    Fill = Brush.Parse(ch.ColorHex ?? "#000000"),
                    Stroke = ch.IsLocked ? (_viewModel.IsDarkMode ? Brushes.LightGray : Brushes.Gray) : Brushes.Transparent,
                    StrokeThickness = ch.IsLocked ? 2 : 0,
                    Tag = ch
                };

                if (_isTutorialActive && _currentTutorialStage == TutorialStage.DragBubble && ch == _viewModel.Project.Characters.FirstOrDefault())
                {
                    ellipse.Stroke = Brush.Parse("#ad87ff");
                    ellipse.StrokeThickness = 2;
                }

                Canvas.SetLeft(ellipse, x);
                Canvas.SetTop(ellipse, y);
                canvas.Children.Add(ellipse);
            }
        }

        private void Canvas_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is not Canvas canvas || canvas.Tag is not Trait trait) return;
            
            var point = e.GetCurrentPoint(canvas);
            if (!point.Properties.IsLeftButtonPressed) return;

            Point clickPos = point.Position;

            // Find clicked ellipse
            // Iterate backwards to find top-most
            for (int i = canvas.Children.Count - 1; i >= 0; i--)
            {
                if (canvas.Children[i] is Ellipse ellipse && ellipse.Tag is Character ch)
                {
                    var left = Canvas.GetLeft(ellipse);
                    var top = Canvas.GetTop(ellipse);
                    var rect = new Rect(left, top, ellipse.Width, ellipse.Height);

                    if (rect.Contains(clickPos) && !ch.IsLocked)
                    {
                        _draggingTrait = trait;
                        _draggingEllipse = ellipse;
                        _dragStartOffsetX = clickPos.X - left;
                        e.Pointer.Capture(canvas);
                        e.Handled = true;
                        return;
                    }
                }
            }
        }

        private void Canvas_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (_draggingEllipse is null || _draggingTrait is null || sender is not Canvas canvas ||
                _draggingEllipse.Tag is not Character draggingCharacter) return;

            var point = e.GetCurrentPoint(canvas);
            if (!point.Properties.IsLeftButtonPressed) return;

            double usable = System.Math.Max(1, canvas.Bounds.Width);
            double newX = point.Position.X - _dragStartOffsetX;
            newX = System.Math.Max(0, System.Math.Min(newX, usable - _draggingEllipse.Width));
            
            Canvas.SetLeft(_draggingEllipse, newX);
            
            double ratio = newX / (usable - _draggingEllipse.Width);
            double value = double.IsFinite(ratio) ? System.Math.Max(0, System.Math.Min(1, ratio)) : 0.5;
            
            draggingCharacter.TraitPositions[_draggingTrait.Id] = value;
            _viewModel?.SetDirty(true);
        }

        private void Canvas_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_draggingEllipse != null)
            {
                if (_isTutorialActive && _currentTutorialStage == TutorialStage.DragBubble) ProceedToNextTutorialStage();
                _viewModel?.SetDirty(true);
                e.Pointer.Capture(null);
                _draggingTrait = null;
                _draggingEllipse = null;
            }
        }

        private async void ExportImage_Click(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null || _viewModel.Project == null) return;

            var selectionDialog = new ExportSelectionWindow(_viewModel.Project.Characters, _viewModel.IsDarkMode);
            var selectedCharacters = await selectionDialog.ShowDialog<List<Character>>(this);

            if (selectedCharacters == null) return;

            var saveOptions = new FilePickerSaveOptions
            {
                Title = "Export Image",
                DefaultExtension = "png",
                FileTypeChoices = new List<FilePickerFileType>
                {
                    new FilePickerFileType("PNG Image") { Patterns = new[] { "*.png" } }
                }
            };

            var file = await StorageProvider.SaveFilePickerAsync(saveOptions);
            if (file != null)
            {
                try
                {
                    // Avalonia's IStorageFile provides a path property in some implementations, but it's safer to use the path if available or stream.
                    // Our service currently takes a string path.
                    // For local files, TryGetLocalPath() is available in newer Avalonia versions or we can use the path property if it's a local file.
                    // However, IStorageFile.Path is a Uri.
                    
                    string? localPath = file.Path.LocalPath;
                    if (localPath != null)
                    {
                        await _imageExportService.ExportImageAsync(_viewModel.Project, selectedCharacters, _viewModel.IsDarkMode, localPath);
                        var msgBox = new SimpleMessageBox("Image exported successfully!", "Export Complete", SimpleMessageBox.MessageBoxButtons.OK);
                        await msgBox.ShowDialog(this);
                    }
                    else
                    {
                         var msgBox = new SimpleMessageBox("Could not determine local file path for export.", "Export Error", SimpleMessageBox.MessageBoxButtons.OK);
                         await msgBox.ShowDialog(this);
                    }
                }
                catch (Exception ex)
                {
                    var msgBox = new SimpleMessageBox($"An error occurred: {ex.Message}", "Export Error", SimpleMessageBox.MessageBoxButtons.OK);
                    await msgBox.ShowDialog(this);
                }
            }
        }

        private async void ExportNotes_Click(object? sender, RoutedEventArgs e)
        {
            if (_viewModel?.Project?.TraitNotes == null) return;

            var saveOptions = new FilePickerSaveOptions
            {
                Title = "Export Notes",
                DefaultExtension = "txt",
                FileTypeChoices = new List<FilePickerFileType>
                {
                    new FilePickerFileType("Text File") { Patterns = new[] { "*.txt" } }
                }
            };

            var file = await StorageProvider.SaveFilePickerAsync(saveOptions);
            if (file != null)
            {
                try
                {
                    string? localPath = file.Path.LocalPath;
                    if (localPath != null)
                    {
                        var sb = new System.Text.StringBuilder();
                        sb.AppendLine($"Notes for Project: {_viewModel.Project.Name}");
                        sb.AppendLine(new string('-', 30));
                        sb.AppendLine();

                        foreach (var trait in TraitDefinitions.All)
                        {
                            if (_viewModel.Project.TraitNotes.TryGetValue(trait.Id, out var note) && !string.IsNullOrWhiteSpace(note))
                            {
                                sb.AppendLine($"[{trait.Name}]");
                                sb.AppendLine(note);
                                sb.AppendLine();
                                sb.AppendLine(new string('-', 20));
                                sb.AppendLine();
                            }
                        }

                        await File.WriteAllTextAsync(localPath, sb.ToString());
                        
                        var msgBox = new SimpleMessageBox("Notes exported successfully!", "Export Complete", SimpleMessageBox.MessageBoxButtons.OK);
                        await msgBox.ShowDialog(this);
                    }
                }
                catch (Exception ex)
                {
                    var msgBox = new SimpleMessageBox($"An error occurred: {ex.Message}", "Export Error", SimpleMessageBox.MessageBoxButtons.OK);
                    await msgBox.ShowDialog(this);
                }
            }
        }

        private void About_Click(object? sender, RoutedEventArgs e)
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.ShowDialog(this);
        }

        private void Help_Click(object? sender, RoutedEventArgs e)
        {
            var helpWindow = new HelpWindow();
            helpWindow.ShowDialog(this);
        }

        private void ApplyTheme(bool isDarkMode)
        {
            if (Application.Current != null)
            {
                Application.Current.RequestedThemeVariant = isDarkMode ? ThemeVariant.Dark : ThemeVariant.Light;
            }

            var mainToolBarBorder = this.FindControl<Border>("MainToolBarBorder");
            var toolBarDivider1 = this.FindControl<Border>("ToolBarDivider1");
            var toolBarDivider2 = this.FindControl<Border>("ToolBarDivider2");
            var toolBarDivider3 = this.FindControl<Border>("ToolBarDivider3");
            var projectNameTextBox = this.FindControl<TextBox>("ProjectNameTextBox");
            
            var mainList = this.FindControl<ListBox>("MainList");
            var supportingList = this.FindControl<ListBox>("SupportingList");
            var backgroundList = this.FindControl<ListBox>("BackgroundList");
            var mainHeader = this.FindControl<TextBlock>("MainHeader");
            var supportingHeader = this.FindControl<TextBlock>("SupportingHeader");
            var backgroundHeader = this.FindControl<TextBlock>("BackgroundHeader");

            var projectNameLabel = this.FindControl<TextBlock>("ProjectNameLabel");
            var charactersLabel = this.FindControl<TextBlock>("CharactersLabel");
            var characterListButtons = this.FindControl<StackPanel>("CharacterListButtons");

            if (mainToolBarBorder != null)
            {
                mainToolBarBorder.Background = isDarkMode ? new SolidColorBrush(Color.FromRgb(25, 25, 25)) : Brushes.Transparent;
                
                // Apply foreground to toolbar buttons explicitly
                if (mainToolBarBorder.Child is Grid grid)
                {
                    var toolbarForeground = isDarkMode ? Brushes.White : Brushes.Black;
                    foreach (var child in grid.Children)
                    {
                        if (child is StackPanel sp)
                        {
                            foreach (var item in sp.Children)
                            {
                                if (item is Button btn)
                                {
                                    btn.Foreground = toolbarForeground;
                                }
                                else if (item is Grid btnGrid)
                                {
                                    foreach (var btnInGrid in btnGrid.Children.OfType<Button>())
                                    {
                                        btnInGrid.Foreground = toolbarForeground;
                                    }
                                }
                                else if (item is ComboBox combo)
                                {
                                    combo.Foreground = toolbarForeground;
                                }
                            }
                        }
                    }
                }
            }

            var dividerBrush = isDarkMode ? new SolidColorBrush(Color.FromRgb(50, 50, 50)) : new SolidColorBrush(Color.FromRgb(220, 220, 220));
            if (toolBarDivider1 != null) toolBarDivider1.Background = dividerBrush;
            if (toolBarDivider2 != null) toolBarDivider2.Background = dividerBrush;
            if (toolBarDivider3 != null) toolBarDivider3.Background = dividerBrush;

            if (projectNameTextBox != null)
            {
                projectNameTextBox.Background = isDarkMode ? new SolidColorBrush(Color.FromRgb(50, 50, 50)) : Brushes.White;
                projectNameTextBox.BorderBrush = isDarkMode ? new SolidColorBrush(Color.FromRgb(85, 85, 85)) : new SolidColorBrush(Color.Parse("#ABADB3"));
                projectNameTextBox.Foreground = isDarkMode ? Brushes.White : Brushes.Black;
            }

            IBrush listBackground = isDarkMode ? new SolidColorBrush(Color.FromRgb(50, 50, 50)) : Brushes.White;
            IBrush listBorder = isDarkMode ? new SolidColorBrush(Color.FromRgb(85, 85, 85)) : new SolidColorBrush(Color.Parse("#ABADB3"));
            IBrush listForeground = isDarkMode ? Brushes.White : Brushes.Black;

            if (mainList != null) { mainList.Background = listBackground; mainList.BorderBrush = listBorder; mainList.Foreground = listForeground; }
            if (supportingList != null) { supportingList.Background = listBackground; supportingList.BorderBrush = listBorder; supportingList.Foreground = listForeground; }
            if (backgroundList != null) { backgroundList.Background = listBackground; backgroundList.BorderBrush = listBorder; backgroundList.Foreground = listForeground; }

            var headerForeground = isDarkMode ? Brushes.White : Brushes.Black;
            if (mainHeader != null) mainHeader.Foreground = headerForeground;
            if (supportingHeader != null) supportingHeader.Foreground = headerForeground;
            if (backgroundHeader != null) backgroundHeader.Foreground = headerForeground;

            if (projectNameLabel != null) projectNameLabel.Foreground = isDarkMode ? Brushes.White : Brushes.Black;
            if (charactersLabel != null) charactersLabel.Foreground = isDarkMode ? Brushes.White : Brushes.Black;

            // Apply styles to buttons in CharacterListButtons
            if (characterListButtons != null)
            {
                IBrush btnBackground = isDarkMode ? new SolidColorBrush(Color.FromRgb(60, 60, 60)) : new SolidColorBrush(Color.Parse("#F0F0F0"));
                IBrush btnBorder = isDarkMode ? new SolidColorBrush(Color.FromRgb(100, 100, 100)) : new SolidColorBrush(Color.Parse("#CCCCCC"));
                IBrush btnForeground = isDarkMode ? Brushes.White : Brushes.Black;

                foreach (var child in characterListButtons.Children)
                {
                    if (child is Button btn)
                    {
                        btn.Background = btnBackground;
                        btn.BorderBrush = btnBorder;
                        btn.Foreground = btnForeground;
                    }
                    else if (child is Grid grid)
                    {
                        foreach (var innerBtn in grid.Children.OfType<Button>())
                        {
                            innerBtn.Background = btnBackground;
                            innerBtn.BorderBrush = btnBorder;
                            innerBtn.Foreground = btnForeground;
                        }
                    }
                }
            }

            this.Background = isDarkMode ? new SolidColorBrush(Color.FromRgb(25, 25, 25)) : new SolidColorBrush(Colors.WhiteSmoke);

            if (_traitsGrid == null) return;
            _traitsGrid.Background = isDarkMode ? new SolidColorBrush(Color.FromRgb(25, 25, 25)) : new SolidColorBrush(Colors.WhiteSmoke);

            var brushEven = isDarkMode ? new SolidColorBrush(Color.FromRgb(35, 35, 35)) : new SolidColorBrush(Color.FromRgb(240, 240, 240));
            var brushOdd = isDarkMode ? new SolidColorBrush(Color.FromRgb(45, 45, 45)) : new SolidColorBrush(Color.FromRgb(235, 235, 235));
            
            int i = 0;
            foreach (var child in _traitsGrid.Children)
            {
                if (child is Border border)
                {
                    border.Background = (i / 4 + i % 4) % 2 == 0 ? brushEven : brushOdd;
                    
                    if (border.Child is Grid innerGrid)
                    {
                        foreach (var innerChild in innerGrid.Children)
                        {
                            if (innerChild is Grid grid)
                            {
                                foreach (var gridChild in grid.Children)
                                {
                                    if (gridChild is TextBlock tb)
                                    {
                                        if (tb.Classes.Contains("TraitTitle"))
                                            tb.Foreground = isDarkMode ? Brushes.White : Brushes.Black;
                                        else if (tb.Classes.Contains("TraitDescription"))
                                            tb.Foreground = isDarkMode ? Brushes.DarkGray : Brushes.DimGray;
                                        else if (tb.Classes.Contains("TraitLabel"))
                                            tb.Foreground = isDarkMode ? Brushes.LightGray : Brushes.DimGray;
                                    }
                                    else if (gridChild is Button btn && btn.Content is PathIcon icon)
                                    {
                                        icon.Foreground = isDarkMode ? Brushes.LightGray : Brushes.Gray;
                                    }
                                }
                            }
                            else if (innerChild is Canvas canvas)
                            {
                                foreach (var canvasChild in canvas.Children)
                                {
                                    if (canvasChild is Line line)
                                    {
                                        line.Stroke = isDarkMode ? Brushes.LightGray : Brushes.Gray;
                                    }
                                }
                                RedrawBubbles(canvas);
                            }
                        }
                    }
                    i++;
                }
            }

            ApplyNotePopupTheme(isDarkMode);
        }

        private void ApplyNotePopupTheme(bool isDarkMode, TextBox? noteTextBox = null)
        {
            var notePopupBorder = this.FindControl<Border>("NotePopupBorder");
            if (notePopupBorder != null)
            {
                notePopupBorder.Background = isDarkMode ? new SolidColorBrush(Color.FromRgb(25, 25, 25)) : new SolidColorBrush(Colors.WhiteSmoke);
            }

            var notePopupTitle = this.FindControl<TextBlock>("NotePopupTitle");
            if (notePopupTitle != null)
            {
                notePopupTitle.Foreground = isDarkMode ? Brushes.White : Brushes.Black;
            }

            if (noteTextBox == null)
            {
                noteTextBox = this.FindControl<TextBox>("NoteTextBox");
            }

            if (noteTextBox != null)
            {
                var bgColor = isDarkMode ? Color.FromRgb(20, 20, 20) : Colors.White;
                var bgBrush = new SolidColorBrush(bgColor);

                noteTextBox.Background = bgBrush;
                noteTextBox.Foreground = isDarkMode ? Brushes.White : Brushes.Black;
                noteTextBox.BorderBrush = isDarkMode ? new SolidColorBrush(Color.FromRgb(100, 100, 100)) : new SolidColorBrush(Color.Parse("#ABADB3"));

                // Override theme resources to ensure the color persists on Focus and PointerOver
                noteTextBox.Resources["TextControlBackgroundFocused"] = bgBrush;
                noteTextBox.Resources["TextControlBackgroundPointerOver"] = bgBrush;
            }
        }

        private void Donate_Click(object? sender, RoutedEventArgs e) 
        { 
            try 
            { 
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(AppConstants.DonationUrl) { UseShellExecute = true }); 
            } 
            catch (Exception ex) 
            { 
                var msgBox = new SimpleMessageBox($"Could not open the donation link. Error: {ex.Message}", "Error", SimpleMessageBox.MessageBoxButtons.OK);
                msgBox.ShowDialog(this);
            } 
        }

        // Drag and Drop for Character List
        private void CharacterItem_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var point = e.GetCurrentPoint(sender as Control);
            if (point.Properties.IsLeftButtonPressed)
            {
                // Check if the source is a button or inside a button
                if (e.Source is Visual visual && visual.FindAncestorOfType<Button>() != null)
                {
                    _isPotentialDrag = false;
                    return;
                }

                _dragStartPoint = point.Position;
                _isPotentialDrag = true;
            }
        }

        private void CharacterItem_DoubleTapped(object? sender, TappedEventArgs e)
        {
            if (sender is Control control && control.DataContext is Character character)
            {
                if (_viewModel != null)
                {
                    _viewModel.SelectedCharacter = character;
                    if (_viewModel.EditCharacterCommand.CanExecute(null))
                    {
                        _viewModel.EditCharacterCommand.Execute(null);
                    }
                }
            }
        }

        private void MainGrid_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var source = e.Source as Visual;
            var projectNameBox = this.FindControl<TextBox>("ProjectNameTextBox");
            if (projectNameBox != null && projectNameBox.IsFocused)
            {
                if (source != null && !projectNameBox.IsVisualAncestorOf(source) && source != projectNameBox)
                {
                    var mainLayoutGrid = this.FindControl<Grid>("MainLayoutGrid");
                    mainLayoutGrid?.Focus();
                }
            }

            if (source != null && _viewModel != null)
            {
                var item = source.FindAncestorOfType<ListBoxItem>();
                var button = source.FindAncestorOfType<Button>();
                var scrollBar = source.FindAncestorOfType<ScrollBar>();
                
                if (item == null && button == null && scrollBar == null)
                {
                     _viewModel.SelectedCharacter = null;
                }
            }
        }

        private void ProjectNameTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var mainLayoutGrid = this.FindControl<Grid>("MainLayoutGrid");
                mainLayoutGrid?.Focus();
            }
        }

        private async void CharacterItem_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (!_isPotentialDrag) return; // Check flag

            if (sender is not Control control) return;
            var point = e.GetCurrentPoint(control);
            if (point.Properties.IsLeftButtonPressed)
            {
                var diff = _dragStartPoint - point.Position;
                if (Math.Abs(diff.X) > 3 || Math.Abs(diff.Y) > 3)
                {
                    if (control.DataContext is Character character)
                    {
                        _isPotentialDrag = false; // Reset to prevent multiple drags
#pragma warning disable CS0618
                        var dragData = new DataObject();
                        dragData.Set("Character", character);
                        var result = await DragDrop.DoDragDrop(e, dragData, DragDropEffects.Move);
#pragma warning restore CS0618
                    }
                }
            }
        }

        private void CharactersList_DragOver(object? sender, DragEventArgs e)
        {
            e.DragEffects = DragDropEffects.None;

#pragma warning disable CS0618
            var droppedCharacter = e.Data.Get("Character") as Character;
#pragma warning restore CS0618
            if (droppedCharacter == null) return;

            if (sender is ListBox list)
            {
                BubbleSize? expectedSize = null;
                if (list.Name == "MainList") expectedSize = BubbleSize.Large;
                else if (list.Name == "SupportingList") expectedSize = BubbleSize.Medium;
                else if (list.Name == "BackgroundList") expectedSize = BubbleSize.Small;

                if (expectedSize.HasValue && droppedCharacter.Size == expectedSize.Value)
                {
                    e.DragEffects = DragDropEffects.Move;

                    var pos = e.GetPosition(list);
                    var visual = list.GetVisualAt(pos);
                    var targetItem = visual?.FindAncestorOfType<ListBoxItem>();
                    
                    bool isBottom = false;

                    if (targetItem == null)
                    {
                        if (list.ItemCount > 0)
                        {
                             targetItem = list.ContainerFromIndex(list.ItemCount - 1) as ListBoxItem;
                             isBottom = true;
                        }
                    }
                    else
                    {
                        var itemPos = e.GetPosition(targetItem);
                        isBottom = itemPos.Y > targetItem.Bounds.Height / 2;
                    }

                    if (_currentDropTarget != targetItem)
                    {
                        ClearDropIndicator();
                        _currentDropTarget = targetItem;
                    }

                    if (_currentDropTarget != null)
                    {
                        var topIndicator = _currentDropTarget.GetVisualDescendants().OfType<Border>().FirstOrDefault(b => b.Name == "TopIndicator");
                        var bottomIndicator = _currentDropTarget.GetVisualDescendants().OfType<Border>().FirstOrDefault(b => b.Name == "BottomIndicator");

                        if (topIndicator != null) topIndicator.IsVisible = !isBottom;
                        if (bottomIndicator != null) bottomIndicator.IsVisible = isBottom;
                    }
                }
                else
                {
                    ClearDropIndicator();
                }
            }
        }

        private void CharactersList_DragLeave(object? sender, DragEventArgs e)
        {
            ClearDropIndicator();
        }

        private void ClearDropIndicator()
        {
            if (_currentDropTarget != null)
            {
                var topIndicator = _currentDropTarget.GetVisualDescendants().OfType<Border>().FirstOrDefault(b => b.Name == "TopIndicator");
                var bottomIndicator = _currentDropTarget.GetVisualDescendants().OfType<Border>().FirstOrDefault(b => b.Name == "BottomIndicator");
                if (topIndicator != null) topIndicator.IsVisible = false;
                if (bottomIndicator != null) bottomIndicator.IsVisible = false;
                _currentDropTarget = null;
            }
        }

        private void CharactersList_Drop(object? sender, DragEventArgs e)
        {
            ClearDropIndicator();

            if (_viewModel == null) return;
#pragma warning disable CS0618
            var droppedCharacter = e.Data.Get("Character") as Character;
#pragma warning restore CS0618
            if (droppedCharacter == null) return;

            var list = sender as ListBox;
            if (list == null) return;

            // Validate target list matches character size
            BubbleSize? expectedSize = null;
            if (list.Name == "MainList") expectedSize = BubbleSize.Large;
            else if (list.Name == "SupportingList") expectedSize = BubbleSize.Medium;
            else if (list.Name == "BackgroundList") expectedSize = BubbleSize.Small;

            if (!expectedSize.HasValue || droppedCharacter.Size != expectedSize.Value)
            {
                return;
            }

            var pos = e.GetPosition(list);
            var visual = list.GetVisualAt(pos);
            
            ListBoxItem? targetItem = null;
            if (visual is Visual v)
            {
                targetItem = v.FindAncestorOfType<ListBoxItem>();
            }

            if (targetItem != null && targetItem.DataContext is Character targetCharacter && !targetCharacter.Equals(droppedCharacter))
            {
                var targetPos = e.GetPosition(targetItem);
                bool insertAfter = targetPos.Y > targetItem.Bounds.Height / 2;
                PerformReorder(droppedCharacter, targetCharacter, insertAfter);
            }
            else
            {
                // Fallback: Move to end of its group
                // Only if dropped on the correct group
                // We can check if the list's ItemsSource contains characters of the same size
                // Or just rely on PerformReorder logic which checks size
                
                // If dropped in empty space of the correct list, move to end
                // We need to know which list it is.
                // But PerformReorder handles "move to end" if we pass the last item? No.
                
                // Simplified: Just reorder to end
                var group = _viewModel.Project.Characters
                    .Where(c => c.Size == droppedCharacter.Size)
                    .OrderBy(c => c.DisplayOrder)
                    .ToList();

                group.Remove(droppedCharacter);
                group.Add(droppedCharacter);

                for (int i = 0; i < group.Count; i++)
                {
                    group[i].DisplayOrder = i;
                }
                _viewModel.SetDirty(true);
                UpdateGroups();
            }
        }

        private void PerformReorder(Character droppedCharacter, Character targetCharacter, bool insertAfter)
        {
            if (_viewModel?.Project == null) return;
            if (droppedCharacter.Size != targetCharacter.Size) return;

            var group = _viewModel.Project.Characters
                .Where(c => c.Size == droppedCharacter.Size)
                .OrderBy(c => c.DisplayOrder)
                .ToList();

            group.Remove(droppedCharacter);

            int targetIndexInGroup = group.IndexOf(targetCharacter);
            if (targetIndexInGroup == -1)
            {
                group.Add(droppedCharacter);
            }
            else
            {
                int newIndex = insertAfter ? targetIndexInGroup + 1 : targetIndexInGroup;
                group.Insert(newIndex, droppedCharacter);
            }

            for (int i = 0; i < group.Count; i++)
            {
                group[i].DisplayOrder = i;
            }
            _viewModel?.SetDirty(true);
            UpdateGroups();
        }

        private void RedrawBubblesInTraits()
        {
            if (_traitsGrid == null) return;
            foreach (var child in _traitsGrid.Children)
            {
                if (child is Border border && border.Child is Grid inner && inner.Children.OfType<Canvas>().FirstOrDefault() is Canvas canvas)
                {
                    RedrawBubbles(canvas);
                }
            }
        }

        private async System.Threading.Tasks.Task<bool> CheckForUpdatesWithResultAsync()
        {
            var currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0);
            var updateInfo = await _updateService.CheckForUpdateAsync(currentVersion);

            if (updateInfo != null)
            {
                var msgBox = new SimpleMessageBox(
                    $"A new version ({updateInfo.Version}) is available! You are currently using version {currentVersion}.\n\nRelease Notes:\n{updateInfo.ReleaseNotes}\n\nWould you like to go to the download page?",
                    "Update Available",
                    SimpleMessageBox.MessageBoxButtons.YesNo);
                
                await msgBox.ShowDialog(this);

                if (msgBox.Result == Core.Services.DialogResult.Yes)
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = updateInfo.Url, UseShellExecute = true });
                    }
                    catch { }
                }
                return true;
            }
            
            return false;
        }

        private void NoteButton_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Trait trait)
            {
                _currentNoteTrait = trait;
                DimOtherTraits(trait);
                var popup = this.FindControl<Popup>("NotePopup");
                var textBox = this.FindControl<TextBox>("NoteTextBox");
                var title = this.FindControl<TextBlock>("NotePopupTitle");
                
                if (popup != null && textBox != null && title != null)
                {
                    title.Text = $"{trait.Name} - Notes";
                    
                    if (_viewModel?.Project?.TraitNotes != null && 
                        _viewModel.Project.TraitNotes.TryGetValue(trait.Id, out var note))
                    {
                        textBox.Text = note;
                    }
                    else
                    {
                        textBox.Text = string.Empty;
                    }

                    // Target the container border to keep the trait line visible
                    if (btn.Parent is Grid noteContainer && 
                        noteContainer.Parent is Grid titleGrid && 
                        titleGrid.Parent is Grid innerGrid && 
                        innerGrid.Parent is Border containerBorder)
                    {
                        popup.PlacementTarget = containerBorder;
                        popup.Placement = PlacementMode.Bottom;
                        popup.VerticalOffset = 5;
                    }
                    else
                    {
                        popup.PlacementTarget = btn;
                        popup.Placement = PlacementMode.Bottom;
                        popup.VerticalOffset = 0;
                    }

                    popup.IsOpen = true;
                    ApplyNotePopupTheme(_viewModel?.IsDarkMode ?? true, textBox);
                    textBox.Focus();
                }
            }
        }

        private void NoteTextBox_TextChanged(object? sender, TextChangedEventArgs e)
        {
            if (_currentNoteTrait != null && _viewModel?.Project != null && sender is TextBox tb)
            {
                if (_viewModel.Project.TraitNotes == null)
                    _viewModel.Project.TraitNotes = new Dictionary<string, string>();

                string text = tb.Text ?? string.Empty;
                _viewModel.Project.TraitNotes[_currentNoteTrait.Id] = text;
                _viewModel.SetDirty(true);
                
                UpdateNoteIconOpacity(_currentNoteTrait, !string.IsNullOrWhiteSpace(text));

                if (_isTutorialActive && _currentTutorialStage == TutorialStage.TraitNotes && !string.IsNullOrWhiteSpace(text))
                {
                    ProceedToNextTutorialStage();
                }
            }
        }

        private void UpdateNoteIconOpacity(Trait trait, bool hasNote)
        {
            if (_traitsGrid == null) return;
            foreach (var child in _traitsGrid.Children)
            {
                if (child is Border border && border.Child is Grid inner && inner.Children.Count > 0 && inner.Children[0] is Grid titleGrid)
                {
                    foreach (var titleChild in titleGrid.Children)
                    {
                        if (titleChild is Grid noteContainer)
                        {
                            foreach (var noteChild in noteContainer.Children)
                            {
                                if (noteChild is Button btn && btn.Tag == trait)
                                {
                                    btn.Opacity = hasNote ? 1.0 : 0.3;
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void CloseNotePopup_Click(object? sender, RoutedEventArgs e)
        {
            var popup = this.FindControl<Popup>("NotePopup");
            if (popup != null) popup.IsOpen = false;
        }

        private void NotePopup_Closed(object? sender, EventArgs e)
        {
            _currentNoteTrait = null;
            RestoreTraitsOpacity();
        }

        private void DimOtherTraits(Trait activeTrait)
        {
            if (_traitsGrid == null) return;
            foreach (var child in _traitsGrid.Children)
            {
                if (child is Border border)
                {
                    if (border.Tag == activeTrait)
                    {
                        border.Opacity = 1.0;
                    }
                    else
                    {
                        border.Opacity = 0.3;
                    }
                }
            }
        }

        private void RestoreTraitsOpacity()
        {
            if (_traitsGrid == null) return;
            foreach (var child in _traitsGrid.Children)
            {
                if (child is Border border)
                {
                    border.Opacity = 1.0;
                }
            }
        }
    }
}