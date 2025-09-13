using Microsoft.Win32;
using SixteenCoreCharacterMapper.ViewModels;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Net.Http;
using System.Text.Json;
using System.Reflection;
using System.Threading.Tasks;

namespace SixteenCoreCharacterMapper
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _viewModel;

        private Trait? _draggingTrait;
        private Ellipse? _draggingEllipse;
        private double _dragStartOffsetX;
        private Brush _lockedStrokeBrush = Brushes.LightGray;
        private DropIndicatorAdorner? _dropIndicatorAdorner;
        private AdornerLayer? _adornerLayer;

        private Point _dragStartPoint;

        private const string PlaceholderText = "Enter Project Title...";
        private bool _isSettingPlaceholder = false;
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

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainWindowViewModel();
            DataContext = _viewModel;

            _viewModel.RedrawTraitsRequested += RedrawAllTraits;
            _viewModel.ApplyThemeRequested += () => ApplyTheme(_viewModel.IsDarkMode);
            _viewModel.CharacterAdded += OnCharacterAddedForTutorial;
            _viewModel.RefreshCharacterListRequested += () => GetCharactersView()?.Refresh();

            Loaded += async (s, e) =>
            {
                SetPlaceholder();
                InitializeTraits();
                ApplyTheme(_viewModel.IsDarkMode);
                RedrawAllTraits();
                _adornerLayer = AdornerLayer.GetAdornerLayer(CharactersList);

                // Now using the single, flexible method for the initial check.
                await CheckForUpdatesWithResultAsync();
            };
        }

        private ICollectionView? GetCharactersView()
        {
            return (this.FindResource("GroupedCharacters") as CollectionViewSource)?.View;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            const double designWidth = 1800;
            const double designHeight = 900;

            double newWidth = e.NewSize.Width;
            double newHeight = e.NewSize.Height;

            bool isScalingNeeded = newWidth < designWidth || newHeight < designHeight;

            if (isScalingNeeded)
            {
                double scaleX = newWidth / designWidth;
                double scaleY = newHeight / designHeight;
                double scale = Math.Min(scaleX, scaleY);
                RootScaleTransform.ScaleX = scale;
                RootScaleTransform.ScaleY = scale;
            }
            else
            {
                RootScaleTransform.ScaleX = 1;
                RootScaleTransform.ScaleY = 1;
            }
        }

        private void OnCharacterAddedForTutorial(Character newCharacter)
        {
            if (_isTutorialActive && _currentTutorialStage == TutorialStage.AddCharacter)
            {
                ProceedToNextTutorialStage();
            }
        }

        private void SetPlaceholder()
        {
            if (string.IsNullOrWhiteSpace(ProjectNameTextBox.Text))
            {
                _isSettingPlaceholder = true;
                ProjectNameTextBox.Text = PlaceholderText;
                ProjectNameTextBox.FontStyle = FontStyles.Italic;
                ProjectNameTextBox.Foreground = _viewModel.IsDarkMode ? Brushes.DarkGray : Brushes.Gray;
                _isSettingPlaceholder = false;
            }
        }

        private void ApplyNormalTextStyle()
        {
            ProjectNameTextBox.FontStyle = FontStyles.Normal;
            ProjectNameTextBox.Foreground = _viewModel.IsDarkMode ? Brushes.White : Brushes.Black;
        }

        private void ProjectNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSettingPlaceholder) return;

            if (!string.IsNullOrWhiteSpace(ProjectNameTextBox.Text) && ProjectNameTextBox.Text != PlaceholderText)
            {
                ApplyNormalTextStyle();
            }

            if (_isTutorialActive && _currentTutorialStage == TutorialStage.ProjectName && !string.IsNullOrWhiteSpace(ProjectNameTextBox.Text) && ProjectNameTextBox.Text != PlaceholderText)
            {
                ProceedToNextTutorialStage();
            }
        }

        private void ProjectNameTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (_isTutorialActive && _currentTutorialStage == TutorialStage.ProjectName && !string.IsNullOrWhiteSpace(ProjectNameTextBox.Text) && ProjectNameTextBox.Text != PlaceholderText)
            {
                ProceedToNextTutorialStage();
                return;
            }

            if (ProjectNameTextBox.Text == PlaceholderText)
            {
                _isSettingPlaceholder = true;
                ProjectNameTextBox.Text = "";
                ApplyNormalTextStyle();
                _isSettingPlaceholder = false;
            }
        }

        private void ProjectNameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ProjectNameTextBox.Text))
            {
                SetPlaceholder();
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _viewModel.ClosingWindow(e);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                _viewModel.SaveProjectCommand.Execute(null);
            }
        }

        private void InitializeTraits()
        {
            TraitsGrid.Children.Clear();
            TraitsGrid.RowDefinitions.Clear();
            TraitsGrid.ColumnDefinitions.Clear();

            int traitIndex = 0;
            const int columnsPerRow = 4;
            const int totalTraits = 16;
            int totalRows = totalTraits / columnsPerRow;

            for (int i = 0; i < totalRows; i++)
            {
                TraitsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            }
            for (int i = 0; i < columnsPerRow; i++)
            {
                TraitsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            foreach (var trait in TraitDefinitions.All)
            {
                int rowIndex = traitIndex / columnsPerRow;
                int columnIndex = traitIndex % columnsPerRow;

                var containerBorder = new Border { Padding = new Thickness(15, 30, 15, 10) };
                var innerGrid = new Grid();
                innerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                innerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) });
                innerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var titleGrid = new Grid();
                titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.4, GridUnitType.Star) });
                titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.6, GridUnitType.Star) });
                titleGrid.Margin = new Thickness(0, 0, 0, 20);
                var title = new TextBlock { Text = trait.Name, FontWeight = FontWeights.Bold, FontSize = 16, VerticalAlignment = VerticalAlignment.Bottom };
                Grid.SetColumn(title, 0);
                titleGrid.Children.Add(title);
                var description = new TextBlock { Text = trait.Description, FontSize = 9, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, TextAlignment = TextAlignment.Right, TextWrapping = TextWrapping.Wrap };
                Grid.SetColumn(description, 1);
                titleGrid.Children.Add(description);
                innerGrid.Children.Add(titleGrid);
                Grid.SetRow(titleGrid, 0);

                var canvas = new Canvas { SnapsToDevicePixels = true, Tag = trait };
                Grid.SetRow(canvas, 1);
                canvas.MouseLeftButtonDown += Canvas_MouseLeftButtonDown;
                canvas.MouseMove += Canvas_MouseMove;
                canvas.MouseLeftButtonUp += Canvas_MouseLeftButtonUp;
                var baseline = new Line { X1 = 0, X2 = 0, Y1 = 24, Y2 = 24, StrokeThickness = 2 };
                canvas.Children.Add(baseline);
                canvas.SizeChanged += (s, ev) =>
                {
                    baseline.X2 = canvas.ActualWidth;
                    baseline.Y1 = canvas.ActualHeight / 2;
                    baseline.Y2 = canvas.ActualHeight / 2;
                    RedrawAllTraits();
                };
                innerGrid.Children.Add(canvas);

                var labelsGrid = new Grid();
                labelsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.30, GridUnitType.Star) });
                labelsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.40, GridUnitType.Star) });
                labelsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.30, GridUnitType.Star) });
                labelsGrid.Margin = new Thickness(0, 5, 0, 0);
                var leftLabel = new TextBlock { Text = trait.LowLabel, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, FontSize = 9, FontStyle = FontStyles.Italic, TextWrapping = TextWrapping.Wrap };
                Grid.SetColumn(leftLabel, 0);
                var rightLabel = new TextBlock { Text = trait.HighLabel, TextAlignment = TextAlignment.Right, VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Right, FontSize = 9, FontStyle = FontStyles.Italic, TextWrapping = TextWrapping.Wrap };
                Grid.SetColumn(rightLabel, 2);
                labelsGrid.Children.Add(leftLabel);
                labelsGrid.Children.Add(rightLabel);
                innerGrid.Children.Add(labelsGrid);
                Grid.SetRow(labelsGrid, 2);

                containerBorder.Child = innerGrid;
                Grid.SetRow(containerBorder, rowIndex);
                Grid.SetColumn(containerBorder, columnIndex);
                TraitsGrid.Children.Add(containerBorder);
                traitIndex++;
            }
        }

        private void CharactersList_Container_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is DependencyObject source && FindAncestor<StackPanel>(source) is StackPanel panel && panel.Name == "CharacterListButtons")
            {
                return;
            }
            if (FindAncestor<ListViewItem>(e.OriginalSource as DependencyObject) == null)
            {
                CharactersList.SelectedItem = null;
            }
        }

        #region Drag and Drop Logic

        private void ListViewItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void ListViewItem_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition(null);
            Vector diff = _dragStartPoint - mousePos;

            if (e.LeftButton == MouseButtonState.Pressed &&
                (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                 Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                if (FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource) is ListViewItem listViewItem &&
                    listViewItem.DataContext is Character)
                {
                    DragDrop.DoDragDrop(listViewItem, listViewItem.DataContext, DragDropEffects.Move);
                }
            }
        }

        private void CharactersList_DragOver(object sender, DragEventArgs e)
        {
            RemoveDropIndicator();

            if (e.Data.GetData(typeof(Character)) is not Character draggedCharacter)
            {
                return;
            }

            var dropPosition = e.GetPosition(CharactersList);
            var targetItem = FindItemContainerAt(dropPosition);

            if (targetItem != null && targetItem.DataContext is Character targetCharacter)
            {
                // Only show the drop indicator if the dragged character
                // is in the same group as the target character.
                if (draggedCharacter.Size == targetCharacter.Size)
                {
                    _dropIndicatorAdorner = new DropIndicatorAdorner(targetItem);
                    _adornerLayer?.Add(_dropIndicatorAdorner);

                    var positionInItem = e.GetPosition(targetItem);
                    _dropIndicatorAdorner.IsBelow = positionInItem.Y > targetItem.ActualHeight / 2;
                }
            }
        }
        private void CharactersList_Drop(object sender, DragEventArgs e)
        {
            RemoveDropIndicator();

            if (e.Data.GetData(typeof(Character)) is not Character droppedCharacter) return;

            var dropPosition = e.GetPosition(CharactersList);
            var targetItem = FindItemContainerAt(dropPosition);

            if (targetItem != null && targetItem.DataContext is Character targetCharacter && !targetCharacter.Equals(droppedCharacter))
            {
                var positionInItem = e.GetPosition(targetItem);
                bool insertAfter = positionInItem.Y > targetItem.ActualHeight / 2;
                PerformReorder(droppedCharacter, targetCharacter, insertAfter);
            }
            else
            {
                // Fallback: This will now only be hit if the list is empty or the drop is far away from any item.
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

                GetCharactersView()?.Refresh();
                _viewModel.SetDirty(true);
            }
        }

        private ListViewItem? FindItemContainerAt(Point position)
        {
            if (VisualTreeHelper.HitTest(CharactersList, position)?.VisualHit is DependencyObject hit)
            {
                var item = FindAncestor<ListViewItem>(hit);
                if (item != null) return item;
            }

            ListViewItem? nearestItem = null;
            double nearestDistance = double.MaxValue;

            for (int i = 0; i < CharactersList.Items.Count; i++)
            {
                if (CharactersList.ItemContainerGenerator.ContainerFromIndex(i) is ListViewItem container)
                {
                    Rect bounds = container.TransformToAncestor(CharactersList).TransformBounds(new Rect(0, 0, container.ActualWidth, container.ActualHeight));
                    double itemCenterY = bounds.Top + bounds.Height / 2;
                    double distance = Math.Abs(position.Y - itemCenterY);

                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestItem = container;
                    }
                }
            }
            return nearestItem;
        }


        private void PerformReorder(Character droppedCharacter, Character targetCharacter, bool insertAfter)
        {
            if (droppedCharacter.Size != targetCharacter.Size) return;

            var group = _viewModel.Project.Characters
                .Where(c => c.Size == droppedCharacter.Size)
                .OrderBy(c => c.DisplayOrder)
                .ToList();

            group.Remove(droppedCharacter);

            int targetIndexInGroup = group.IndexOf(targetCharacter);
            if (targetIndexInGroup == -1)
            {
                group.Add(droppedCharacter); // Failsafe
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

            GetCharactersView()?.Refresh();
            _viewModel.SetDirty(true);
        }

        private void CharactersList_DragLeave(object sender, DragEventArgs e)
        {
            var relativePosition = e.GetPosition(CharactersList);
            if (relativePosition.X < 0 || relativePosition.Y < 0 ||
                relativePosition.X >= CharactersList.ActualWidth ||
                relativePosition.Y >= CharactersList.ActualHeight)
            {
                RemoveDropIndicator();
            }
        }

        private void RemoveDropIndicator()
        {
            if (_adornerLayer != null && _dropIndicatorAdorner != null)
            {
                _adornerLayer.Remove(_dropIndicatorAdorner);
                _dropIndicatorAdorner = null;
            }
        }

        #endregion

        private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
        {
            do { if (current is T ancestor) return ancestor; current = VisualTreeHelper.GetParent(current); } while (current != null);
            return null;
        }

        private void ApplyTheme(bool isDarkMode)
        {
            var lightToolBarStyle = (Style)FindResource("ToolBarButtonStyle");
            var darkToolBarStyle = (Style)FindResource("DarkToolBarButtonStyle");
            var lightCharListStyle = (Style)FindResource("CharacterListButtonStyle");
            var darkCharListStyle = (Style)FindResource("DarkCharacterListButtonStyle");

            var activeToolBarStyle = isDarkMode ? darkToolBarStyle : lightToolBarStyle;
            var activeCharListStyle = isDarkMode ? darkCharListStyle : lightCharListStyle;

            _lockedStrokeBrush = isDarkMode ? Brushes.LightGray : Brushes.DarkGray;

            MainToolBar.Background = isDarkMode ? new SolidColorBrush(Color.FromRgb(25, 25, 25)) : Brushes.Transparent;
            if (MainToolBar.Items.Count > 0 && MainToolBar.Items[0] is Grid toolBarGrid)
            {
                foreach (var panel in toolBarGrid.Children.OfType<StackPanel>())
                {
                    foreach (var item in panel.Children)
                    {
                        if (item is Button button) button.Style = activeToolBarStyle;
                        else if (item is Grid grid)
                        {
                            foreach (var btn in grid.Children.OfType<Button>()) btn.Style = activeToolBarStyle;
                        }
                    }
                }
            }

            foreach (var item in CharacterListButtons.Children)
            {
                if (item is Button button) button.Style = activeCharListStyle;
                else if (item is Grid grid)
                {
                    foreach (var btn in grid.Children.OfType<Button>()) btn.Style = activeCharListStyle;
                }
            }

            Background = isDarkMode ? new SolidColorBrush(Color.FromRgb(25, 25, 25)) : new SolidColorBrush(Colors.WhiteSmoke);
            Foreground = isDarkMode ? Brushes.White : Brushes.Black;
            ToolBarDivider.BorderBrush = isDarkMode ? new SolidColorBrush(Color.FromRgb(50, 50, 50)) : new SolidColorBrush(Color.FromRgb(220, 220, 220));
            TraitsGrid.Background = isDarkMode ? new SolidColorBrush(Color.FromRgb(25, 25, 25)) : new SolidColorBrush(Colors.WhiteSmoke);
            ProjectNameLabel.Foreground = isDarkMode ? Brushes.White : Brushes.Black;
            CharactersLabel.Foreground = isDarkMode ? Brushes.White : Brushes.Black;

            ProjectNameTextBox.Background = isDarkMode ? new SolidColorBrush(Color.FromRgb(50, 50, 50)) : Brushes.White;
            ProjectNameTextBox.BorderBrush = isDarkMode ? new SolidColorBrush(Color.FromRgb(85, 85, 85)) : new SolidColorBrush(Color.FromRgb(171, 173, 179));
            CharactersList.Background = isDarkMode ? new SolidColorBrush(Color.FromRgb(50, 50, 50)) : Brushes.White;
            CharactersList.Foreground = isDarkMode ? Brushes.White : Brushes.Black;
            CharactersList.BorderBrush = isDarkMode ? new SolidColorBrush(Color.FromRgb(85, 85, 85)) : new SolidColorBrush(Color.FromRgb(171, 173, 179));

            Resources["LockedIconImageSource"] = new BitmapImage(new Uri($"pack://application:,,,/Assets/lock_{(isDarkMode ? "dark" : "light")}.png"));
            Resources["UnlockedIconImageSource"] = new BitmapImage(new Uri($"pack://application:,,,/Assets/unlock_{(isDarkMode ? "dark" : "light")}.png"));
            Resources["VisibleIconImageSource"] = new BitmapImage(new Uri($"pack://application:,,,/Assets/eye_open_{(isDarkMode ? "dark" : "light")}.png"));
            Resources["HiddenIconImageSource"] = new BitmapImage(new Uri($"pack://application:,,,/Assets/eye_closed_{(isDarkMode ? "dark" : "light")}.png"));

            var brushEven = isDarkMode ? new SolidColorBrush(Color.FromRgb(35, 35, 35)) : new SolidColorBrush(Color.FromArgb(5, 0, 0, 0));
            var brushOdd = isDarkMode ? new SolidColorBrush(Color.FromRgb(45, 45, 45)) : new SolidColorBrush(Color.FromArgb(10, 0, 0, 0));
            foreach (var border in TraitsGrid.Children.OfType<Border>())
            {
                var traitIndex = TraitsGrid.Children.IndexOf(border);
                border.Background = (traitIndex / 4 + traitIndex % 4) % 2 == 0 ? brushEven : brushOdd;
                if (border.Child is Grid innerGrid)
                {
                    var titleGrid = innerGrid.Children.OfType<Grid>().FirstOrDefault(g => Grid.GetRow(g) == 0);
                    if (titleGrid != null)
                    {
                        titleGrid.Children.OfType<TextBlock>().FirstOrDefault(tb => Grid.GetColumn(tb) == 0)?.SetCurrentValue(ForegroundProperty, isDarkMode ? Brushes.White : Brushes.Black);
                        titleGrid.Children.OfType<TextBlock>().FirstOrDefault(tb => Grid.GetColumn(tb) == 1)?.SetCurrentValue(ForegroundProperty, isDarkMode ? Brushes.DarkGray : Brushes.Gray);
                    }
                    innerGrid.Children.OfType<Grid>().FirstOrDefault(g => Grid.GetRow(g) == 2)?.Children.OfType<TextBlock>().ToList().ForEach(l => l.SetCurrentValue(ForegroundProperty, isDarkMode ? Brushes.LightGray : Brushes.Gray));
                    if (innerGrid.Children.OfType<Canvas>().FirstOrDefault()?.Children.OfType<Line>().FirstOrDefault() is Line baseline) baseline.Stroke = isDarkMode ? Brushes.LightGray : Brushes.DarkGray;
                }
            }

            if (ProjectNameTextBox.Text == PlaceholderText) ProjectNameTextBox.Foreground = isDarkMode ? Brushes.DarkGray : Brushes.Gray;
            else ProjectNameTextBox.Foreground = isDarkMode ? Brushes.White : Brushes.Black;

            RedrawAllTraits();
        }

        private void RedrawAllTraits()
        {
            if (TraitsGrid == null) return;
            foreach (var border in TraitsGrid.Children.OfType<Border>())
            {
                if (border.Child is not Grid innerGrid) continue;
                if (innerGrid.Children.OfType<Canvas>().FirstOrDefault() is not Canvas canvas) continue;
                if (canvas.Tag is not Trait trait) continue;

                canvas.Children.OfType<Ellipse>().Where(el => el.Tag is Character).ToList().ForEach(el => canvas.Children.Remove(el));

                foreach (var ch in _viewModel.Project.Characters)
                {
                    if (!ch.IsVisible) continue;
                    if (!ch.TraitPositions.ContainsKey(trait.Name)) ch.TraitPositions[trait.Name] = 0.5;

                    double pos = ch.TraitPositions[trait.Name];
                    double size = ch.Size switch { BubbleSize.Large => 40, BubbleSize.Medium => 25, _ => 15 };
                    double usable = Math.Max(1, canvas.ActualWidth);
                    double x = pos * (usable - size);
                    double y = (canvas.ActualHeight - size) / 2;

                    var ellipse = new Ellipse
                    {
                        Tag = ch,
                        Width = size,
                        Height = size,
                        Fill = (SolidColorBrush)(new BrushConverter().ConvertFromString(ch.ColorHex))!,
                        Stroke = ch.IsLocked ? _lockedStrokeBrush : Brushes.Transparent,
                        StrokeThickness = ch.IsLocked ? 3 : 0
                    };

                    if (_isTutorialActive && _currentTutorialStage == TutorialStage.DragBubble && ch == _viewModel.Project.Characters.FirstOrDefault())
                    {
                        ellipse.Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ad87ff"));
                        ellipse.StrokeThickness = 2;
                    }

                    Canvas.SetLeft(ellipse, x);
                    Canvas.SetTop(ellipse, y);
                    canvas.Children.Add(ellipse);
                }
            }
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Canvas canvas || canvas.Tag is not Trait trait) return;
            Point clickPos = e.GetPosition(canvas);
            for (int i = canvas.Children.Count - 1; i >= 0; i--)
            {
                if (canvas.Children[i] is Ellipse ellipse && ellipse.Tag is Character ch)
                {
                    if (new Rect(Canvas.GetLeft(ellipse), Canvas.GetTop(ellipse), ellipse.Width, ellipse.Height).Contains(clickPos) && !ch.IsLocked)
                    {
                        var draggingCharacter = ch;
                        _draggingTrait = trait;
                        _draggingEllipse = ellipse;
                        _dragStartOffsetX = clickPos.X - Canvas.GetLeft(ellipse);
                        canvas.CaptureMouse();
                        return;
                    }
                }
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_draggingEllipse is null || _draggingTrait is null || sender is not Canvas canvas ||
                _draggingEllipse.Tag is not Character draggingCharacter) return;

            double usable = Math.Max(1, canvas.ActualWidth);
            double newX = e.GetPosition(canvas).X - _dragStartOffsetX;
            newX = Math.Max(0, Math.Min(newX, usable - _draggingEllipse.Width));
            Canvas.SetLeft(_draggingEllipse, newX);
            double ratio = newX / (usable - _draggingEllipse.Width);
            draggingCharacter.TraitPositions[_draggingTrait.Name] = double.IsFinite(ratio) ? Math.Max(0, Math.Min(1, ratio)) : 0.5;
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_draggingEllipse?.Tag is Character)
            {
                if (_isTutorialActive && _currentTutorialStage == TutorialStage.DragBubble) ProceedToNextTutorialStage();
                _viewModel.SetDirty(true);
            }
            (sender as Canvas)?.ReleaseMouseCapture();
            _draggingTrait = null;
            _draggingEllipse = null;
        }

        private void Character_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListViewItem)
            {
                _viewModel.EditCharacterCommand.Execute(null);
            }
        }

        private void CharactersList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                _viewModel.DeleteCharacterCommand.Execute(null);
            }
        }

        private void IconButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Button button && button.DataContext is Character character)
            {
                if (button.Name == "LockButton")
                {
                    _viewModel.ToggleLockCommand.Execute(character);
                    if (_isTutorialActive && _currentTutorialStage == TutorialStage.LockCharacter)
                    {
                        if (character.IsLocked) _tutorialLockToggled = true; else _tutorialUnlockToggled = true;
                        if (_tutorialLockToggled && _tutorialUnlockToggled) ProceedToNextTutorialStage();
                    }
                }
                else if (button.Name == "VisibilityButton")
                {
                    _viewModel.ToggleVisibilityCommand.Execute(character);
                    if (_isTutorialActive && _currentTutorialStage == TutorialStage.ShowHideCharacter)
                    {
                        if (!character.IsVisible) _tutorialHideToggled = true; else _tutorialShowToggled = true;
                        if (_tutorialHideToggled && _tutorialShowToggled) ProceedToNextTutorialStage();
                    }
                }
                e.Handled = true;
            }
        }

        #region Tutorial Logic

        private void ToggleThemeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isTutorialActive && _currentTutorialStage == TutorialStage.ToggleTheme)
            {
                ProceedToNextTutorialStage();
            }
        }

        private void Tutorial_Click(object sender, RoutedEventArgs e) { if (_isTutorialActive) StopTutorial(); else StartTutorial(); }
        private void StartTutorial() { _isTutorialActive = true; _currentTutorialStage = TutorialStage.None; ProceedToNextTutorialStage(); }
        private void StopTutorial() { _isTutorialActive = false; _currentTutorialStage = TutorialStage.None; ProjectNameHighlight.Visibility = Visibility.Collapsed; CharacterButtonsHighlight.Visibility = Visibility.Collapsed; ToggleThemeHighlight.Visibility = Visibility.Collapsed; ExportImageHighlight.Visibility = Visibility.Collapsed; TutorialPopup.IsOpen = false; if (CharactersList.Items.Count > 0 && GetListViewItem(CharactersList, 0) is ListViewItem item) { FindVisualChild<Border>(item, "LockHighlight")?.SetCurrentValue(VisibilityProperty, Visibility.Collapsed); FindVisualChild<Border>(item, "VisibilityHighlight")?.SetCurrentValue(VisibilityProperty, Visibility.Collapsed); } RedrawAllTraits(); }
        private void ProceedToNextTutorialStage() { if (!_isTutorialActive) return; ProjectNameHighlight.Visibility = Visibility.Collapsed; CharacterButtonsHighlight.Visibility = Visibility.Collapsed; ToggleThemeHighlight.Visibility = Visibility.Collapsed; ExportImageHighlight.Visibility = Visibility.Collapsed; if (CharactersList.Items.Count > 0 && GetListViewItem(CharactersList, 0) is ListViewItem item) { FindVisualChild<Border>(item, "LockHighlight")?.SetCurrentValue(VisibilityProperty, Visibility.Collapsed); FindVisualChild<Border>(item, "VisibilityHighlight")?.SetCurrentValue(VisibilityProperty, Visibility.Collapsed); } RedrawAllTraits(); _currentTutorialStage++; ShowTutorialStage(_currentTutorialStage); }
        private void ShowTutorialStage(TutorialStage stage)
        {
            TutorialPopup.IsOpen = false;
            TutorialButton.Visibility = Visibility.Collapsed;
            switch (stage)
            {
                case TutorialStage.ProjectName: TutorialTextBlock.Text = "Enter your project title here."; TutorialPopup.PlacementTarget = ProjectNameTextBox; ProjectNameHighlight.Visibility = Visibility.Visible; break;
                case TutorialStage.AddCharacter: TutorialTextBlock.Text = _viewModel.Project.Characters.Any() ? "Add another character here!\n\nOn the window that opens, enter a name and pick a color.\nThen select your character type, on which depends the size of the character's bubble on the chart.\nFinally, click OK." : "Add your first character here!\n\nOn the window that opens, enter a name and pick a color.\nThen select your character type, on which depends the size of the character's bubble on the chart.\nFinally, click OK."; TutorialPopup.PlacementTarget = AddCharacterButton; CharacterButtonsHighlight.Visibility = Visibility.Visible; break;
                case TutorialStage.DragBubble: if (!_viewModel.Project.Characters.Any()) { StopTutorial(); return; } TutorialTextBlock.Text = "Drag your character's bubble on the 16 trait lines."; Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(() => { Ellipse? firstBubble = null; if (TraitsGrid.Children.OfType<Border>().FirstOrDefault()?.Child is Grid innerGrid && innerGrid.Children.OfType<Canvas>().FirstOrDefault() is Canvas canvas) firstBubble = canvas.Children.OfType<Ellipse>().FirstOrDefault(el => el.Tag is Character); if (firstBubble != null) { TutorialPopup.PlacementTarget = firstBubble; TutorialPopup.Placement = PlacementMode.Right; TutorialPopup.IsOpen = true; } else { TutorialPopup.Placement = PlacementMode.Center; TutorialPopup.PlacementTarget = TraitsGrid; TutorialPopup.IsOpen = true; } })); RedrawAllTraits(); return;
                case TutorialStage.LockCharacter: Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(() => { _tutorialLockToggled = false; _tutorialUnlockToggled = false; TutorialTextBlock.Text = "You can lock and unlock the position of your character's bubbles here.\n\nTry it now!"; if (GetListViewItem(CharactersList, 0) is ListViewItem firstItem && FindVisualChild<Button>(firstItem, "LockButton") is Button lockButton && FindVisualChild<Border>(firstItem, "LockHighlight") is Border lockHighlight) { TutorialPopup.PlacementTarget = lockButton; TutorialPopup.Placement = PlacementMode.Right; lockHighlight.Visibility = Visibility.Visible; TutorialPopup.IsOpen = true; } else { StopTutorial(); } })); return;
                case TutorialStage.ShowHideCharacter: Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(() => { _tutorialHideToggled = false; _tutorialShowToggled = false; TutorialTextBlock.Text = "You can hide and show your character's bubbles here.\n\nTry it now!"; if (GetListViewItem(CharactersList, 0) is ListViewItem item && FindVisualChild<Button>(item, "VisibilityButton") is Button visibilityButton && FindVisualChild<Border>(item, "VisibilityHighlight") is Border visibilityHighlight) { TutorialPopup.PlacementTarget = visibilityButton; TutorialPopup.Placement = PlacementMode.Right; visibilityHighlight.Visibility = Visibility.Visible; TutorialPopup.IsOpen = true; } else { StopTutorial(); } })); return;
                case TutorialStage.ToggleTheme: TutorialTextBlock.Text = "You can toggle between Light and Dark Mode here.\n\nTry it now!"; TutorialPopup.PlacementTarget = ToggleThemeButton; TutorialPopup.Placement = PlacementMode.Bottom; ToggleThemeHighlight.Visibility = Visibility.Visible; break;
                case TutorialStage.ExportImage: ToggleThemeHighlight.Visibility = Visibility.Collapsed; TutorialTextBlock.Text = "You can export your character map to a PNG image here."; TutorialPopup.PlacementTarget = ExportImageButton; TutorialPopup.Placement = PlacementMode.Bottom; ExportImageHighlight.Visibility = Visibility.Visible; TutorialButton.Visibility = Visibility.Visible; TutorialButton.Content = "Got it!"; break;
                case TutorialStage.Final: ExportImageHighlight.Visibility = Visibility.Collapsed; TutorialTextBlock.Text = "Add more characters and drag their bubbles to map their similarities and differences.\n\nThat's all!"; TutorialPopup.PlacementTarget = CharactersList; TutorialPopup.Placement = PlacementMode.Right; TutorialButton.Visibility = Visibility.Visible; TutorialButton.Content = "Finish tutorial"; break;
                default: StopTutorial(); break;
            }
            TutorialPopup.IsOpen = true;
        }
        private void TutorialButton_Click(object sender, RoutedEventArgs e) { if (_currentTutorialStage == TutorialStage.ExportImage) ProceedToNextTutorialStage(); else if (_currentTutorialStage == TutorialStage.Final) StopTutorial(); }
        private T? FindVisualChild<T>(DependencyObject? parent, string name) where T : FrameworkElement
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T element && element.Name == name) return element;
                var result = FindVisualChild<T>(child, name);
                if (result != null) return result;
            }
            return null;
        }
        private ListViewItem? GetListViewItem(ListView listView, int index) { if (index < 0 || index >= listView.Items.Count) return null; return listView.ItemContainerGenerator.ContainerFromIndex(index) as ListViewItem; }

        #endregion

        #region Unchanged UI Logic (Export, About, Donate)
        // In MainWindow.xaml.cs

        // In MainWindow.xaml.cs

        private void ExportImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string projectName = (ProjectNameTextBox.Text == PlaceholderText) ? "Untitled Project" : ProjectNameTextBox.Text.Trim();
                if (string.IsNullOrEmpty(projectName)) projectName = "UntitledCharacterMap";

                var saveFileDialog = new SaveFileDialog { Filter = "PNG Image|*.png", FileName = projectName + ".png" };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // --- Define layout size and final render quality ---
                    const double layoutWidth = 1920;
                    const double layoutHeight = 1194;
                    const double renderDpi = 150; // Set to 150 for a high-quality compromise
                    const double systemDpi = 96;  // WPF's native DPI
                    double scale = layoutWidth / 1600.0; // The scale factor from the original 1600px base

                    var imageDarkBackgroundColor = Color.FromRgb(25, 25, 25);
                    var imageLightBackgroundColor = Colors.WhiteSmoke;

                    var exportContainerGrid = new Grid();
                    exportContainerGrid.Background = _viewModel.IsDarkMode ? new SolidColorBrush(imageDarkBackgroundColor) : new SolidColorBrush(imageLightBackgroundColor);
                    exportContainerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    exportContainerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    var tempTraitsGrid = new Grid { Width = 1600 * scale, Height = 900 * scale };
                    Grid.SetRow(tempTraitsGrid, 0);

                    int traitIndex = 0;
                    const int columnsPerRow = 4;
                    for (int i = 0; i < 4; i++) tempTraitsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    for (int i = 0; i < columnsPerRow; i++) tempTraitsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    foreach (var trait in TraitDefinitions.All)
                    {
                        int rowIndex = traitIndex / columnsPerRow;
                        int columnIndex = traitIndex % columnsPerRow;
                        var containerBorder = new Border { Padding = new Thickness(15 * scale, 30 * scale, 15 * scale, 10 * scale) };
                        ApplyBorderTheme(containerBorder, traitIndex);
                        var innerGrid = new Grid();
                        innerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                        innerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50 * scale) });
                        innerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                        var titleGrid = new Grid();
                        titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        titleGrid.Margin = new Thickness(0, 0, 0, 25 * scale);
                        var title = new TextBlock { Text = trait.Name, FontWeight = FontWeights.Bold, FontSize = 16 * scale, VerticalAlignment = VerticalAlignment.Bottom, Foreground = _viewModel.IsDarkMode ? Brushes.White : Brushes.Black };
                        Grid.SetColumn(title, 0);
                        titleGrid.Children.Add(title);
                        var description = new TextBlock { Text = trait.Description, FontSize = 9 * scale, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Foreground = _viewModel.IsDarkMode ? Brushes.DarkGray : Brushes.Gray };
                        Grid.SetColumn(description, 1);
                        titleGrid.Children.Add(description);
                        innerGrid.Children.Add(titleGrid);
                        Grid.SetRow(titleGrid, 0);
                        var canvas = new Canvas { SnapsToDevicePixels = true, Tag = trait };
                        Grid.SetRow(canvas, 1);
                        var baseline = new Line { X1 = 0, X2 = (1600 * scale) / columnsPerRow - (30 * scale), Y1 = 25 * scale, Y2 = 25 * scale, StrokeThickness = 2 * scale };
                        ApplyLineTheme(baseline);
                        canvas.Children.Add(baseline);
                        innerGrid.Children.Add(canvas);
                        var labelsGrid = new Grid();
                        labelsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.25, GridUnitType.Star) });
                        labelsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.5, GridUnitType.Star) });
                        labelsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.25, GridUnitType.Star) });
                        labelsGrid.Margin = new Thickness(0, 5 * scale, 0, 0);
                        var leftLabel = new TextBlock { Text = trait.LowLabel, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, FontSize = 9 * scale, FontStyle = FontStyles.Italic, TextWrapping = TextWrapping.Wrap };
                        ApplyTextBlockTheme(leftLabel);
                        Grid.SetColumn(leftLabel, 0);
                        var rightLabel = new TextBlock { Text = trait.HighLabel, TextAlignment = TextAlignment.Right, VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Right, FontSize = 9 * scale, FontStyle = FontStyles.Italic, TextWrapping = TextWrapping.Wrap };
                        ApplyTextBlockTheme(rightLabel);
                        Grid.SetColumn(rightLabel, 2);
                        labelsGrid.Children.Add(leftLabel);
                        labelsGrid.Children.Add(rightLabel);
                        innerGrid.Children.Add(labelsGrid);
                        Grid.SetRow(labelsGrid, 2);
                        containerBorder.Child = innerGrid;
                        Grid.SetRow(containerBorder, rowIndex);
                        Grid.SetColumn(containerBorder, columnIndex);
                        tempTraitsGrid.Children.Add(containerBorder);
                        double canvasWidth = tempTraitsGrid.Width / columnsPerRow;
                        double canvasHeight = 50 * scale;
                        foreach (var ch in _viewModel.Project.Characters)
                        {
                            if (!ch.IsVisible) continue;
                            if (!ch.TraitPositions.ContainsKey(trait.Name)) ch.TraitPositions[trait.Name] = 0.5;
                            double pos = Math.Max(0, Math.Min(1, ch.TraitPositions[trait.Name]));
                            double size = ch.Size switch { BubbleSize.Large => 40 * scale, BubbleSize.Medium => 25 * scale, _ => 15 * scale };
                            double x = pos * (canvasWidth - (30 * scale) - size);
                            double y = (canvasHeight - size) / 2;
                            var fillBrush = (SolidColorBrush)(new BrushConverter().ConvertFromString(ch.ColorHex))!;
                            var ellipse = new Ellipse { Width = size, Height = size, Fill = fillBrush, Stroke = ch.IsLocked ? (_viewModel.IsDarkMode ? Brushes.LightGray : Brushes.DarkGray) : Brushes.Transparent, StrokeThickness = ch.IsLocked ? 3 * scale : 0, Tag = ch };
                            Canvas.SetLeft(ellipse, x);
                            Canvas.SetTop(ellipse, y);
                            canvas.Children.Add(ellipse);
                        }
                        traitIndex++;
                    }
                    exportContainerGrid.Children.Add(tempTraitsGrid);

                    var bottomRowGrid = new Grid
                    {
                        Width = 1600 * scale,
                        VerticalAlignment = VerticalAlignment.Top,
                        Margin = new Thickness(0, 25 * scale, 0, 0)
                    };
                    Grid.SetRow(bottomRowGrid, 1);
                    bottomRowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    bottomRowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    exportContainerGrid.Children.Add(bottomRowGrid);

                    var legendWrapPanel = new WrapPanel
                    {
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(15 * scale, 0, 0, 0)
                    };
                    var sortedCharacters = _viewModel.Project.Characters.OrderBy(ch => ch.Size switch { BubbleSize.Large => 0, BubbleSize.Medium => 1, _ => 2 });
                    foreach (var ch in sortedCharacters)
                    {
                        double size = ch.Size switch { BubbleSize.Large => 40 * scale, BubbleSize.Medium => 25 * scale, _ => 15 * scale };
                        var legendItemPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 35 * scale, 10 * scale), VerticalAlignment = VerticalAlignment.Center, };
                        var ellipse = new Ellipse { Width = size, Height = size, Fill = (SolidColorBrush)(new BrushConverter().ConvertFromString(ch.ColorHex))!, Margin = new Thickness(0, 0, 10 * scale, 0), };
                        var textBlock = new TextBlock { Text = ch.Name, Foreground = _viewModel.IsDarkMode ? Brushes.White : Brushes.Black, VerticalAlignment = VerticalAlignment.Center, FontSize = 11 * scale };
                        legendItemPanel.Children.Add(ellipse);
                        legendItemPanel.Children.Add(textBlock);
                        legendWrapPanel.Children.Add(legendItemPanel);
                    }
                    Grid.SetColumn(legendWrapPanel, 0);
                    bottomRowGrid.Children.Add(legendWrapPanel);

                    var watermarkPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 15 * scale, 0),
                        Opacity = 0.7
                    };
                    var watermarkLogo = new Image
                    {
                        Width = 20 * scale,
                        Height = 20 * scale,
                        Source = new BitmapImage(new Uri("pack://application:,,,/Assets/16Core logo icon v2.png")),
                        Margin = new Thickness(0, 0, 5 * scale, 0)
                    };
                    var watermarkText = new TextBlock
                    {
                        Text = "Made with 16Core Character Mapper",
                        Foreground = _viewModel.IsDarkMode ? new SolidColorBrush(Color.FromArgb(255, 150, 150, 150)) : new SolidColorBrush(Color.FromArgb(255, 100, 100, 100)),
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = 9 * scale
                    };
                    watermarkPanel.Children.Add(watermarkLogo);
                    watermarkPanel.Children.Add(watermarkText);
                    Grid.SetColumn(watermarkPanel, 1);
                    bottomRowGrid.Children.Add(watermarkPanel);

                    // Calculate the final pixel dimensions for the high-DPI bitmap
                    int finalPixelWidth = (int)(layoutWidth * renderDpi / systemDpi);
                    int finalPixelHeight = (int)(layoutHeight * renderDpi / systemDpi);

                    exportContainerGrid.Width = layoutWidth;
                    exportContainerGrid.Height = layoutHeight;
                    exportContainerGrid.Measure(new Size(layoutWidth, layoutHeight));
                    exportContainerGrid.Arrange(new Rect(0, 0, layoutWidth, layoutHeight));
                    exportContainerGrid.UpdateLayout();

                    RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
                        finalPixelWidth,
                        finalPixelHeight,
                        renderDpi,
                        renderDpi,
                        PixelFormats.Pbgra32);

                    renderBitmap.Render(exportContainerGrid);

                    PngBitmapEncoder pngEncoder = new PngBitmapEncoder();
                    pngEncoder.Frames.Add(BitmapFrame.Create(renderBitmap));
                    using (FileStream fileStream = new FileStream(saveFileDialog.FileName, FileMode.Create)) pngEncoder.Save(fileStream);
                    MessageBox.Show("Image exported successfully!", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while exporting the image: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void About_Click(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new Window
            {
                Title = "About 16Core Character Mapper",
                Width = 550,
                Height = 615,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow,
                Background = this.Background,
                Foreground = this.Foreground
            };

            var mainGrid = new Grid { Margin = new Thickness(20) };
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Content
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer

            // --- Header ---
            var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 20) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(headerGrid, 0);

            var logo = new Image
            {
                Width = 64,
                Height = 64,
                Source = new BitmapImage(new Uri("pack://application:,,,/Assets/16Core logo icon v2.png")),
                Margin = new Thickness(0, 0, 15, 0),
                VerticalAlignment = VerticalAlignment.Top
            };
            Grid.SetColumn(logo, 0);
            headerGrid.Children.Add(logo);

            var titlePanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(titlePanel, 1);
            titlePanel.Children.Add(new TextBlock { Text = "16Core Character Mapper", FontWeight = FontWeights.Bold, FontSize = 16 });
            titlePanel.Children.Add(new TextBlock { Text = "Version 1.0.1" }); // Updated version
            headerGrid.Children.Add(titlePanel);
            mainGrid.Children.Add(headerGrid);

            // --- Content ---
            var contentScrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = new StackPanel { Margin = new Thickness(0, 0, 0, 15) }
            };
            Grid.SetRow(contentScrollViewer, 1);
            var contentPanel = (StackPanel)contentScrollViewer.Content;

            // Helper to add text blocks
            Action<string, bool, double> addText = (text, isBold, topMargin) =>
            {
                contentPanel.Children.Add(new TextBlock
                {
                    Text = text,
                    TextWrapping = TextWrapping.Wrap,
                    FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal,
                    Margin = new Thickness(0, topMargin, 0, 5)
                });
            };

            // Helper for hyperlinks
            Action<string, string, string> addLink = (prefix, linkText, url) =>
            {
                var tb = new TextBlock { Margin = new Thickness(0, 5, 0, 5) };
                tb.Inlines.Add(prefix);
                var hyperlink = new Hyperlink(new Run(linkText)) { NavigateUri = new Uri(url) };
                hyperlink.RequestNavigate += (s, args) => { Process.Start(new ProcessStartInfo { FileName = args.Uri.AbsoluteUri, UseShellExecute = true }); args.Handled = true; };
                tb.Inlines.Add(hyperlink);
                contentPanel.Children.Add(tb);
            };

            addText("A Tool for Storytellers", true, 0);
            addText("16Core Character Mapper helps you develop deep characters by mapping their personalities across 16 core traits, letting you visualize how they interact with the world and each other.", false, 0);

            addText("Our Approach to Personality Traits", true, 15);
            addText("This tool is inspired by established psychological trait theories. To ensure this application remains free and accessible to all, it exclusively uses public-domain terminology primarily drawn from the International Personality Item Pool (IPIP).", false, 0);

            addText("Disclaimer:", true, 15);
            addText("This is a creative tool, not a scientific or diagnostic instrument. It is not intended for psychological assessment.", false, 0);

            contentPanel.Children.Add(new Separator { Margin = new Thickness(0, 10, 0, 10) });

            addText("Support & Feedback", true, 0);

            var donationTb = new TextBlock { Margin = new Thickness(0, 5, 0, 5), TextWrapping = TextWrapping.Wrap };
            donationTb.Inlines.Add("16Core Character Mapper is offered as free donationware. If you find it useful, please consider ");
            var donationLink = new Hyperlink(new Run("a donation via paypal.com")) { NavigateUri = new Uri("https://www.paypal.com/donate/?hosted_button_id=9QWZ6U22CL9KA") };
            donationLink.RequestNavigate += (s, args) => { Process.Start(new ProcessStartInfo { FileName = args.Uri.AbsoluteUri, UseShellExecute = true }); args.Handled = true; };
            donationTb.Inlines.Add(donationLink);
            donationTb.Inlines.Add(".");
            contentPanel.Children.Add(donationTb);

            addLink("For questions, support, or feedback, please contact: ", "16core@georgetsirogiannis.com", "mailto:16core@georgetsirogiannis.com");
            addLink("Official Website: ", "16corecharactermapper.my.canva.site", "https://16corecharactermapper.my.canva.site/");

            contentPanel.Children.Add(new Separator { Margin = new Thickness(0, 10, 0, 10) });

            contentPanel.Children.Add(new TextBlock { Text = "© 2025 George Tsirogiannis. All Rights Reserved.", HorizontalAlignment = HorizontalAlignment.Center, FontSize = 10 });

            mainGrid.Children.Add(contentScrollViewer);

            // --- Footer (Buttons) ---
            var buttonGrid = new Grid { Margin = new Thickness(0, 15, 0, 0) };
            Grid.SetRow(buttonGrid, 2);
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var donateButton = new Button
            {
                Content = "Donate",
                Padding = new Thickness(10, 5, 10, 5),
                HorizontalAlignment = HorizontalAlignment.Left,
                Style = _viewModel.IsDarkMode ? (Style)FindResource("DarkToolBarButtonStyle")! : (Style)FindResource("ToolBarButtonStyle")!
            };
            donateButton.Click += (s, args) => { try { Process.Start(new ProcessStartInfo("https://www.paypal.com/donate/?hosted_button_id=9QWZ6U22CL9KA") { UseShellExecute = true }); } catch (Exception ex) { MessageBox.Show($"Could not open the donation link. Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); } };
            Grid.SetColumn(donateButton, 0);
            buttonGrid.Children.Add(donateButton);

            var checkUpdatesButton = new Button
            {
                Content = "Check for Updates",
                Padding = new Thickness(10, 5, 10, 5),
                HorizontalAlignment = HorizontalAlignment.Right,
                Style = _viewModel.IsDarkMode ? (Style)FindResource("DarkToolBarButtonStyle")! : (Style)FindResource("ToolBarButtonStyle")!
            };
            checkUpdatesButton.Click += async (s, args) =>
            {
                bool isUpdateAvailable = await CheckForUpdatesWithResultAsync();

                if (!isUpdateAvailable)
                {
                    MessageBox.Show("You're using the latest version available.", "No Updates", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            };
            Grid.SetColumn(checkUpdatesButton, 2);
            buttonGrid.Children.Add(checkUpdatesButton);

            var okButton = new Button
            {
                Content = "OK",
                IsDefault = true,
                Width = 80,
                Padding = new Thickness(5),
                HorizontalAlignment = HorizontalAlignment.Right,
                Style = _viewModel.IsDarkMode ? (Style)FindResource("DarkToolBarButtonStyle")! : (Style)FindResource("ToolBarButtonStyle")!
            };
            okButton.Click += (s, args) => aboutWindow.Close();
            Grid.SetColumn(okButton, 3);
            buttonGrid.Children.Add(okButton);
            mainGrid.Children.Add(buttonGrid);

            aboutWindow.Content = mainGrid;
            aboutWindow.ShowDialog();
        }

        private async Task<bool> CheckForUpdatesWithResultAsync()
        {
            // IMPORTANT: Replace with the RAW URL to your version.json file on GitHub
            string url = "https://raw.githubusercontent.com/georgetsirogiannis/SixteenCoreCharacterMapper/master/version.json";

            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "16Core-Character-Mapper-Update-Check");

                    string json = await httpClient.GetStringAsync(url);
                    var updateInfo = JsonSerializer.Deserialize<UpdateInfo>(json);

                    if (updateInfo?.Version == null || updateInfo.Url == null || updateInfo.ReleaseNotes == null) return false;

                    var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
                    var latestVersion = new Version(updateInfo.Version);

                    if (latestVersion > currentVersion)
                    {
                        var result = MessageBox.Show(
                            $"A new version ({latestVersion}) is available! You are currently using version {currentVersion}.\n\nRelease Notes:\n{updateInfo.ReleaseNotes}\n\nWould you like to go to the download page?",
                            "Update Available",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Information);

                        if (result == MessageBoxResult.Yes)
                        {
                            Process.Start(new ProcessStartInfo { FileName = updateInfo.Url, UseShellExecute = true });
                        }
                        return true; // An update was found
                    }
                    else
                    {
                        return false; // No update was found
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Update check failed: {ex.Message}");
                return false; // An error occurred, so no update was found
            }
        }

        private void Donate_Click(object sender, RoutedEventArgs e) { try { Process.Start(new ProcessStartInfo("https://www.paypal.com/donate/?hosted_button_id=9QWZ6U22CL9KA") { UseShellExecute = true }); } catch (Exception ex) { MessageBox.Show($"Could not open the donation link. Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); } }
        private void ApplyBorderTheme(Border border, int traitIndex) { var brushEven = _viewModel.IsDarkMode ? new SolidColorBrush(Color.FromRgb(35, 35, 35)) : new SolidColorBrush(Color.FromArgb(5, 0, 0, 0)); var brushOdd = _viewModel.IsDarkMode ? new SolidColorBrush(Color.FromRgb(45, 45, 45)) : new SolidColorBrush(Color.FromArgb(10, 0, 0, 0)); border.Background = (traitIndex / 4 + traitIndex % 4) % 2 == 0 ? brushEven : brushOdd; }
        private void ApplyTextBlockTheme(TextBlock textBlock) { textBlock.Foreground = _viewModel.IsDarkMode ? Brushes.White : Brushes.Black; }
        private void ApplyLineTheme(Line line) { line.Stroke = _viewModel.IsDarkMode ? Brushes.LightGray : Brushes.DarkGray; }
        #endregion
    }

    internal class DropIndicatorAdorner : Adorner
    {
        private bool _isBelow;
        private readonly Pen _pen;

        public bool IsBelow
        {
            get => _isBelow;
            set
            {
                if (_isBelow != value)
                {
                    _isBelow = value;
                    InvalidateVisual();
                }
            }
        }

        public DropIndicatorAdorner(UIElement adornedElement) : base(adornedElement)
        {
            IsHitTestVisible = false;
            _pen = new Pen(Brushes.DodgerBlue, 2);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (AdornedElement is not FrameworkElement adornedElement) return;

            double y = IsBelow ? adornedElement.ActualHeight : 0;
            drawingContext.DrawLine(_pen, new Point(0, y), new Point(adornedElement.ActualWidth, y));
        }
    }

    public class UpdateInfo
    {
        public string? Version { get; set; }
        public string? Url { get; set; }
        public string? ReleaseNotes { get; set; }
    }
}