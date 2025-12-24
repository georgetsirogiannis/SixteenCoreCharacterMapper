using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Controls.Shapes;
using global::Avalonia.Layout;
using global::Avalonia.Media;
using global::Avalonia.Media.Imaging;
using SixteenCoreCharacterMapper.Core;
using SixteenCoreCharacterMapper.Core.Models;
using SixteenCoreCharacterMapper.Core.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SixteenCoreCharacterMapper.Avalonia.Services
{
    public class AvaloniaImageExportService : IImageExportService
    {
        public async Task ExportImageAsync(Project project, IEnumerable<Character> charactersToExport, bool isDarkMode, string filePath)
        {
            await global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                const double layoutWidth = 1920;
                const double layoutHeight = 1194;
                double scale = layoutWidth / 1600.0;

                var imageDarkBackgroundColor = Color.FromRgb(25, 25, 25);
                var imageLightBackgroundColor = Colors.WhiteSmoke;

                var exportContainerGrid = new Grid();
                exportContainerGrid.Width = layoutWidth;
                exportContainerGrid.Height = layoutHeight;
                exportContainerGrid.Background = isDarkMode ? new SolidColorBrush(imageDarkBackgroundColor) : new SolidColorBrush(imageLightBackgroundColor);
                
                exportContainerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                exportContainerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

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
                    var containerBorder = new Border { Padding = new Thickness(15 * scale, 22 * scale, 15 * scale, 10 * scale) };
                    
                    var brushEven = isDarkMode ? new SolidColorBrush(Color.FromRgb(35, 35, 35)) : new SolidColorBrush(Color.FromRgb(240, 240, 240));
                    var brushOdd = isDarkMode ? new SolidColorBrush(Color.FromRgb(45, 45, 45)) : new SolidColorBrush(Color.FromRgb(235, 235, 235));
                    containerBorder.Background = (traitIndex / 4 + traitIndex % 4) % 2 == 0 ? brushEven : brushOdd;

                    var innerGrid = new Grid();
                    innerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    innerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50 * scale) });
                    innerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    
                    var titleGrid = new Grid();
                    titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.4, GridUnitType.Star) });
                    titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.6, GridUnitType.Star) });
                    titleGrid.Margin = new Thickness(0, 0, 0, 25 * scale);
                    titleGrid.MinHeight = (15 * scale) * 2 * 1.25;

                    var title = new TextBlock
                    {
                        Text = trait.Name,
                        FontWeight = FontWeight.Bold,
                        FontSize = 15 * scale,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Foreground = isDarkMode ? Brushes.White : Brushes.Black,
                        TextWrapping = TextWrapping.Wrap,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        LineHeight = (15 * scale) * 1.25
                    };
                    Grid.SetColumn(title, 0);
                    titleGrid.Children.Add(title);
                    
                    var description = new TextBlock { Text = trait.Description, FontSize = 9 * scale, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Foreground = isDarkMode ? Brushes.DarkGray : Brushes.DimGray, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Right };
                    Grid.SetColumn(description, 1);
                    titleGrid.Children.Add(description);
                    innerGrid.Children.Add(titleGrid);
                    Grid.SetRow(titleGrid, 0);
                    
                    var canvas = new Canvas { Tag = trait };
                    Grid.SetRow(canvas, 1);
                    var baseline = new Line { StartPoint = new Point(0, 25 * scale), EndPoint = new Point((1600 * scale) / columnsPerRow - (30 * scale), 25 * scale), StrokeThickness = 2 * scale };
                    baseline.Stroke = isDarkMode ? Brushes.LightGray : Brushes.Gray;
                    canvas.Children.Add(baseline);
                    innerGrid.Children.Add(canvas);
                    
                    var labelsGrid = new Grid();
                    labelsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.33, GridUnitType.Star) });
                    labelsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.34, GridUnitType.Star) });
                    labelsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.33, GridUnitType.Star) });
                    labelsGrid.Margin = new Thickness(0, 5 * scale, 0, 0);
                    
                    var leftLabel = new TextBlock { Text = trait.LowLabel, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, FontSize = 9 * scale, FontStyle = FontStyle.Italic, TextWrapping = TextWrapping.Wrap, Foreground = isDarkMode ? Brushes.White : Brushes.Black };
                    Grid.SetColumn(leftLabel, 0);
                    var rightLabel = new TextBlock { Text = trait.HighLabel, TextAlignment = TextAlignment.Right, VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Right, FontSize = 9 * scale, FontStyle = FontStyle.Italic, TextWrapping = TextWrapping.Wrap, Foreground = isDarkMode ? Brushes.White : Brushes.Black, Padding = new Thickness(0, 0, 3 * scale, 0) };
                    Grid.SetColumn(rightLabel, 2);
                    labelsGrid.Children.Add(leftLabel);
                    labelsGrid.Children.Add(rightLabel);
                    innerGrid.Children.Add(labelsGrid);
                    Grid.SetRow(labelsGrid, 2);
                    
                    containerBorder.Child = innerGrid;
                    Grid.SetRow(containerBorder, rowIndex);
                    Grid.SetColumn(containerBorder, columnIndex);
                    tempTraitsGrid.Children.Add(containerBorder);
                    
                    double canvasWidth = (1600 * scale) / columnsPerRow;
                    double canvasHeight = 50 * scale;
                    foreach (var ch in charactersToExport)
                    {
                        if (!ch.IsVisible) continue;
                        double pos = ch.GetTraitPosition(trait);

                        double bubbleSize = ch.Size switch { BubbleSize.Large => 40 * scale, BubbleSize.Medium => 26 * scale, _ => 16 * scale };
                        double x = pos * (canvasWidth - (30 * scale) - bubbleSize);
                        double y = (canvasHeight - bubbleSize) / 2;
                        
                        var fillBrush = Brush.Parse(ch.ColorHex ?? "#000000");
                        var ellipse = new Ellipse { Width = bubbleSize, Height = bubbleSize, Fill = fillBrush, Stroke = ch.IsLocked ? (isDarkMode ? Brushes.LightGray : Brushes.Gray) : Brushes.Transparent, StrokeThickness = ch.IsLocked ? 3 * scale : 0, Tag = ch };
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
                bottomRowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                exportContainerGrid.Children.Add(bottomRowGrid);

                var legendWrapPanel = new WrapPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(15 * scale, 0, 0, 0)
                };
                var sortedCharacters = charactersToExport.OrderBy(ch => ch.Size switch { BubbleSize.Large => 0, BubbleSize.Medium => 1, _ => 2 });
                foreach (var ch in sortedCharacters)
                {
                    double bubbleSize = ch.Size switch { BubbleSize.Large => 40 * scale, BubbleSize.Medium => 26 * scale, _ => 16 * scale };
                    var legendItemPanel = new StackPanel 
                    { 
                        Orientation = Orientation.Horizontal, 
                        Margin = new Thickness(0, 0, 20 * scale, 10 * scale), 
                        VerticalAlignment = VerticalAlignment.Center, 
                    };
                    var ellipse = new Ellipse { Width = bubbleSize, Height = bubbleSize, Fill = Brush.Parse(ch.ColorHex ?? "#000000"), Margin = new Thickness(0, 0, 10 * scale, 0), };
                    var textBlock = new TextBlock 
                    { 
                        Text = ch.Name, 
                        Foreground = isDarkMode ? Brushes.White : Brushes.Black, 
                        VerticalAlignment = VerticalAlignment.Center, 
                        FontSize = 11 * scale,
                        TextTrimming = TextTrimming.CharacterEllipsis
                    };
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
                    Height = 20 * scale,
                    Margin = new Thickness(0, 0, 5 * scale, 0),
                    Stretch = Stretch.Uniform
                };

                try
                {
                    watermarkLogo.Source = new Bitmap(global::Avalonia.Platform.AssetLoader.Open(new Uri("avares://SixteenCoreCharacterMapper.Avalonia/Assets/16Core%20logo%20icon%20v2.png")));
                }
                catch
                {
                    // Ignore if logo is missing
                }

                var watermarkText = new TextBlock
                {
                    Text = AppConstants.WatermarkText,
                    Foreground = isDarkMode ? new SolidColorBrush(Color.Parse("#969696")) : new SolidColorBrush(Color.Parse("#646464")),
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 9 * scale
                };
                watermarkPanel.Children.Add(watermarkLogo);
                watermarkPanel.Children.Add(watermarkText);
                Grid.SetColumn(watermarkPanel, 1);
                bottomRowGrid.Children.Add(watermarkPanel);

                var size = new Size(layoutWidth, layoutHeight);
                exportContainerGrid.Measure(size);
                exportContainerGrid.Arrange(new Rect(size));

                var pixelSize = new PixelSize((int)(layoutWidth * 150 / 96), (int)(layoutHeight * 150 / 96));
                using var bitmap = new RenderTargetBitmap(pixelSize, new Vector(150, 150));
                bitmap.Render(exportContainerGrid);

                using var memoryStream = new MemoryStream();
                bitmap.Save(memoryStream);
                memoryStream.Position = 0;

                using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
                await memoryStream.CopyToAsync(fileStream);
            });
        }
    }
}
