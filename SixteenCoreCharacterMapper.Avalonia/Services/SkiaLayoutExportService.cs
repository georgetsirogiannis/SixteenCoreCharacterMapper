using SkiaSharp;
using SixteenCoreCharacterMapper.Core.Models;
using SixteenCoreCharacterMapper.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Platform;

namespace SixteenCoreCharacterMapper.Avalonia.Services
{
    public class SkiaLayoutExportService
    {
        public void Export(Window window, string filePath, bool isSvg, bool isHtml = false)
        {
            var bounds = window.Bounds;
            double width = bounds.Width;
            double height = bounds.Height;

            if (isHtml)
            {
                using var exporter = new HtmlExporter(width, height);
                TraverseVisualTree(exporter, window, window, 1.0);
                exporter.DrawWatermark(width, height);
                File.WriteAllText(filePath, exporter.GetHtml());
            }
            else
            {
                using var stream = new FileStream(filePath, FileMode.Create);
                using var wStream = new SKManagedWStream(stream);
                
                SKCanvas canvas;
                SKDocument? document = null;
                
                if (isSvg)
                {
                    var svgCanvas = SKSvgCanvas.Create(new SKRect(0, 0, (float)width, (float)height), wStream);
                    canvas = svgCanvas;
                }
                else
                {
                    document = SKDocument.CreatePdf(wStream);
                    canvas = document.BeginPage((float)width, (float)height);
                }

                using var exporter = new SkiaExporter(canvas);
                try
                {
                    TraverseVisualTree(exporter, window, window, 1.0);
                    exporter.DrawWatermark(width, height);
                }
                finally
                {
                    if (document != null)
                    {
                        document.EndPage();
                        document.Close();
                        document.Dispose();
                    }
                    else
                    {
                        canvas.Dispose();
                    }
                }
            }
        }

        private void TraverseVisualTree(ILayoutExporter exporter, Visual visual, Visual root, double opacity)
        {
            if (!visual.IsVisible) return;

            double currentOpacity = opacity * visual.Opacity;
            if (currentOpacity <= 0) return;

            var bounds = visual.Bounds;
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            var topLeft = visual.TranslatePoint(new Point(0, 0), root);
            if (!topLeft.HasValue) return;

            var rect = new Rect(topLeft.Value.X, topLeft.Value.Y, bounds.Width, bounds.Height);

            bool clipped = false;
            if (visual.ClipToBounds)
            {
                exporter.PushClip(rect);
                clipped = true;
            }

            try
            {
                DrawVisual(exporter, visual, rect, currentOpacity);

                foreach (var child in visual.GetVisualChildren())
                {
                    if (child is Visual v)
                    {
                        TraverseVisualTree(exporter, v, root, currentOpacity);
                    }
                }
            }
            finally
            {
                if (clipped)
                {
                    exporter.PopClip();
                }
            }
        }

        private void DrawVisual(ILayoutExporter exporter, Visual visual, Rect rect, double opacity)
        {
            if (visual is Border border)
            {
                exporter.DrawRectangle(rect, border.Background, border.BorderBrush, border.BorderThickness.Left, border.CornerRadius, opacity);
            }
            else if (visual is Panel panel && panel.Background != null)
            {
                exporter.DrawRectangle(rect, panel.Background, null, 0, default, opacity);
            }
            else if (visual is ContentControl cc && cc.Background != null)
            {
                exporter.DrawRectangle(rect, cc.Background, null, 0, default, opacity);
            }

            if (visual is Shape shape)
            {
                if (shape is Ellipse)
                {
                    exporter.DrawEllipse(rect, shape.Fill, shape.Stroke, shape.StrokeThickness, opacity);
                }
                else if (shape is Rectangle)
                {
                    exporter.DrawRectangle(rect, shape.Fill, shape.Stroke, shape.StrokeThickness, default, opacity);
                }
                else if (shape is Line line)
                {
                    var start = new Point(rect.X + line.StartPoint.X, rect.Y + line.StartPoint.Y);
                    var end = new Point(rect.X + line.EndPoint.X, rect.Y + line.EndPoint.Y);
                    exporter.DrawLine(start, end, shape.Stroke, shape.StrokeThickness, opacity, rect);
                }
                else if (shape is global::Avalonia.Controls.Shapes.Path path)
                {
                    exporter.DrawPath(path, rect, opacity);
                }
            }

            if (visual is TextBlock tb && !string.IsNullOrEmpty(tb.Text))
            {
                exporter.DrawText(rect, tb.Text, tb, opacity);
            }
            else if (visual is TextPresenter tp && tp.TemplatedParent is TextBox parentTextBox)
            {
                exporter.DrawText(rect, parentTextBox.Text ?? "", parentTextBox, opacity);
            }
            else if (visual is AccessText at && !string.IsNullOrEmpty(at.Text))
            {
                string text = at.Text.Replace("_", "");
                exporter.DrawText(rect, text, at, opacity);
            }
        }

        private interface ILayoutExporter : IDisposable
        {
            void DrawRectangle(Rect rect, IBrush? fill, IBrush? stroke, double strokeThickness, CornerRadius cornerRadius, double opacity);
            void DrawEllipse(Rect rect, IBrush? fill, IBrush? stroke, double strokeThickness, double opacity);
            void DrawLine(Point start, Point end, IBrush? stroke, double strokeThickness, double opacity, Rect bounds);
            void DrawPath(global::Avalonia.Controls.Shapes.Path path, Rect rect, double opacity);
            void DrawText(Rect rect, string text, Control control, double opacity);
            void PushClip(Rect rect);
            void PopClip();
            void DrawWatermark(double canvasWidth, double canvasHeight);
        }

        private class SkiaExporter : ILayoutExporter
        {
            private readonly SKCanvas _canvas;

            public SkiaExporter(SKCanvas canvas)
            {
                _canvas = canvas;
            }

            public void Dispose() { }

            public void DrawRectangle(Rect rect, IBrush? fill, IBrush? stroke, double strokeThickness, CornerRadius cornerRadius, double opacity)
            {
                using var paint = new SKPaint { IsAntialias = true };
                var skRect = ToSKRect(rect);

                if (fill != null)
                {
                    ApplyBrush(paint, fill, opacity);
                    paint.Style = SKPaintStyle.Fill;
                    if (cornerRadius != default)
                        _canvas.DrawRoundRect(skRect, (float)cornerRadius.TopLeft, (float)cornerRadius.TopLeft, paint);
                    else
                        _canvas.DrawRect(skRect, paint);
                }

                if (stroke != null && strokeThickness > 0)
                {
                    ApplyBrush(paint, stroke, opacity);
                    paint.Style = SKPaintStyle.Stroke;
                    paint.StrokeWidth = (float)strokeThickness;
                    if (cornerRadius != default)
                        _canvas.DrawRoundRect(skRect, (float)cornerRadius.TopLeft, (float)cornerRadius.TopLeft, paint);
                    else
                        _canvas.DrawRect(skRect, paint);
                }
            }

            public void DrawEllipse(Rect rect, IBrush? fill, IBrush? stroke, double strokeThickness, double opacity)
            {
                using var paint = new SKPaint { IsAntialias = true };
                var skRect = ToSKRect(rect);

                if (fill != null)
                {
                    ApplyBrush(paint, fill, opacity);
                    paint.Style = SKPaintStyle.Fill;
                    _canvas.DrawOval(skRect, paint);
                }

                if (stroke != null && strokeThickness > 0)
                {
                    ApplyBrush(paint, stroke, opacity);
                    paint.Style = SKPaintStyle.Stroke;
                    paint.StrokeWidth = (float)strokeThickness;
                    _canvas.DrawOval(skRect, paint);
                }
            }

            public void DrawLine(Point start, Point end, IBrush? stroke, double strokeThickness, double opacity, Rect bounds)
            {
                if (stroke == null || strokeThickness <= 0) return;
                using var paint = new SKPaint { IsAntialias = true };
                ApplyBrush(paint, stroke, opacity);
                paint.Style = SKPaintStyle.Stroke;
                paint.StrokeWidth = (float)strokeThickness;
                _canvas.DrawLine((float)start.X, (float)start.Y, (float)end.X, (float)end.Y, paint);
            }

            public void DrawPath(global::Avalonia.Controls.Shapes.Path path, Rect rect, double opacity)
            {
                if (path.Data == null) return;
                using var paint = new SKPaint { IsAntialias = true };
                var skPath = SKPath.ParseSvgPathData(path.Data.ToString());
                if (skPath == null) return;

                var pathBounds = skPath.Bounds;
                var matrix = SKMatrix.CreateTranslation((float)rect.X, (float)rect.Y);

                if (path.Stretch != Stretch.None && pathBounds.Width > 0 && pathBounds.Height > 0)
                {
                    float scaleX = (float)rect.Width / pathBounds.Width;
                    float scaleY = (float)rect.Height / pathBounds.Height;

                    if (path.Stretch == Stretch.Uniform)
                    {
                        float scale = Math.Min(scaleX, scaleY);
                        scaleX = scale;
                        scaleY = scale;
                    }
                    else if (path.Stretch == Stretch.UniformToFill)
                    {
                        float scale = Math.Max(scaleX, scaleY);
                        scaleX = scale;
                        scaleY = scale;
                    }
                    var scaleMatrix = SKMatrix.CreateScale(scaleX, scaleY);
                    matrix = SKMatrix.Concat(matrix, scaleMatrix);
                }

                using var transformedPath = new SKPath();
                skPath.Transform(matrix, transformedPath);

                if (path.Fill != null)
                {
                    ApplyBrush(paint, path.Fill, opacity);
                    paint.Style = SKPaintStyle.Fill;
                    _canvas.DrawPath(transformedPath, paint);
                }
                if (path.Stroke != null && path.StrokeThickness > 0)
                {
                    ApplyBrush(paint, path.Stroke, opacity);
                    paint.Style = SKPaintStyle.Stroke;
                    paint.StrokeWidth = (float)path.StrokeThickness;
                    _canvas.DrawPath(transformedPath, paint);
                }
            }

            public void DrawText(Rect rect, string text, Control control, double opacity)
            {
                using var paint = new SKPaint { IsAntialias = true };
                var foreground = control.GetValue(TextBlock.ForegroundProperty);
                var fontFamily = control.GetValue(TextBlock.FontFamilyProperty);
                var fontSize = control.GetValue(TextBlock.FontSizeProperty);
                var fontWeight = control.GetValue(TextBlock.FontWeightProperty);
                var fontStyle = control.GetValue(TextBlock.FontStyleProperty);
                var textAlignment = control.GetValue(TextBlock.TextAlignmentProperty);

                ApplyBrush(paint, foreground, opacity);
                paint.Style = SKPaintStyle.Fill;

                var skWeight = (SKFontStyleWeight)(int)fontWeight;
                var skSlant = fontStyle == FontStyle.Italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright;
                
                SKTypeface? typeface = null;
                if (fontFamily != null)
                    typeface = SKTypeface.FromFamilyName(fontFamily.Name, skWeight, SKFontStyleWidth.Normal, skSlant);
                if (typeface == null)
                    typeface = SKTypeface.FromFamilyName("Arial", skWeight, SKFontStyleWidth.Normal, skSlant);

                paint.Typeface = typeface;
                paint.TextSize = (float)fontSize;

                var textBounds = new SKRect();
                paint.MeasureText(text, ref textBounds);

                float x = (float)rect.X;
                if (textAlignment == TextAlignment.Center) x = (float)rect.Center.X - (textBounds.Width / 2);
                else if (textAlignment == TextAlignment.Right) x = (float)rect.Right - textBounds.Width;

                float y = (float)rect.Y + paint.TextSize;
                if (rect.Height > paint.TextSize * 1.5)
                {
                    var vert = control.GetValue(Layoutable.VerticalAlignmentProperty);
                    if (vert == VerticalAlignment.Center)
                        y = (float)rect.Center.Y + (paint.TextSize / 2) - (textBounds.Bottom / 2);
                    else if (vert == VerticalAlignment.Bottom)
                        y = (float)rect.Bottom - textBounds.Bottom;
                }

                _canvas.DrawText(text, x, y, paint);
            }

            public void PushClip(Rect rect)
            {
                _canvas.Save();
                _canvas.ClipRect(ToSKRect(rect));
            }

            public void PopClip()
            {
                _canvas.Restore();
            }

            public void DrawWatermark(double canvasWidth, double canvasHeight)
            {
                using var paint = new SKPaint { IsAntialias = true, TextSize = 12, Color = new SKColor(100, 100, 100) };
                string text = AppConstants.WatermarkText;
                var textWidth = paint.MeasureText(text);

                SKBitmap? logo = null;
                try
                {
                    var uri = new Uri("avares://SixteenCoreCharacterMapper.Avalonia/Assets/16Core%20logo%20icon%20v2.png");
                    using var stream = global::Avalonia.Platform.AssetLoader.Open(uri);
                    using var ms = new MemoryStream();
                    stream.CopyTo(ms);
                    logo = SKBitmap.Decode(ms.ToArray());
                }
                catch { }

                float logoWidth = 0;
                float logoHeight = 24;
                if (logo != null)
                {
                    logoWidth = logoHeight * ((float)logo.Width / logo.Height);
                }

                float padding = 10;
                float totalWidth = textWidth + (logo != null ? logoWidth + 5 : 0);

                float x = (float)canvasWidth - totalWidth - padding;
                float y = (float)canvasHeight - padding;

                _canvas.DrawText(text, x, y, paint);
                x += textWidth + 5;

                if (logo != null)
                {
                    var destRect = new SKRect(x, y - logoHeight + 4, x + logoWidth, y + 4);
                    _canvas.DrawBitmap(logo, destRect, paint);
                    logo.Dispose();
                }
            }

            private SKRect ToSKRect(Rect r) => new SKRect((float)r.X, (float)r.Y, (float)r.Right, (float)r.Bottom);

            private void ApplyBrush(SKPaint paint, IBrush? brush, double opacity)
            {
                if (brush is ISolidColorBrush solid)
                {
                    var c = solid.Color;
                    paint.Color = new SKColor(c.R, c.G, c.B, (byte)(c.A * opacity));
                }
                else
                {
                    paint.Color = new SKColor(0, 0, 0, (byte)(255 * opacity));
                }
            }
        }

        private class HtmlExporter : ILayoutExporter
        {
            private readonly StringBuilder _sb = new();
            private readonly Stack<Point> _originStack = new();
            private Point _currentOrigin = new(0, 0);
            private readonly double _width;
            private readonly double _height;

            public HtmlExporter(double width, double height)
            {
                _width = width;
                _height = height;
                _sb.AppendLine("<!DOCTYPE html><html><head><style>body { margin: 0; overflow: hidden; } div { box-sizing: border-box; }</style></head><body>");
                _sb.AppendLine($"<div style='position: relative; width: {width}px; height: {height}px;'>");
            }

            public string GetHtml()
            {
                _sb.AppendLine("</div></body></html>");
                return _sb.ToString();
            }

            public void Dispose() { }

            private string GetColor(IBrush? brush, double opacity)
            {
                if (brush is ISolidColorBrush solid)
                {
                    var c = solid.Color;
                    return $"rgba({c.R}, {c.G}, {c.B}, {(c.A / 255.0) * opacity})";
                }
                return "transparent";
            }

            private string GetRectStyle(Rect rect, double opacity)
            {
                double left = rect.X - _currentOrigin.X;
                double top = rect.Y - _currentOrigin.Y;
                return $"position: absolute; left: {left}px; top: {top}px; width: {rect.Width}px; height: {rect.Height}px; opacity: {opacity};";
            }

            public void DrawRectangle(Rect rect, IBrush? fill, IBrush? stroke, double strokeThickness, CornerRadius cornerRadius, double opacity)
            {
                var style = GetRectStyle(rect, opacity);
                if (fill != null) style += $"background-color: {GetColor(fill, 1.0)};";
                if (stroke != null && strokeThickness > 0) style += $"border: {strokeThickness}px solid {GetColor(stroke, 1.0)};";
                if (cornerRadius != default) style += $"border-radius: {cornerRadius.TopLeft}px;";

                _sb.AppendLine($"<div style='{style}'></div>");
            }

            public void DrawEllipse(Rect rect, IBrush? fill, IBrush? stroke, double strokeThickness, double opacity)
            {
                var style = GetRectStyle(rect, opacity);
                style += "border-radius: 50%;";
                if (fill != null) style += $"background-color: {GetColor(fill, 1.0)};";
                if (stroke != null && strokeThickness > 0) style += $"border: {strokeThickness}px solid {GetColor(stroke, 1.0)};";
                
                _sb.AppendLine($"<div style='{style}'></div>");
            }

            public void DrawLine(Point start, Point end, IBrush? stroke, double strokeThickness, double opacity, Rect bounds)
            {
                // Use SVG for line
                double left = bounds.X - _currentOrigin.X;
                double top = bounds.Y - _currentOrigin.Y;
                
                // Line coordinates are absolute, need to be relative to bounds (which is the SVG container)
                double x1 = start.X - bounds.X;
                double y1 = start.Y - bounds.Y;
                double x2 = end.X - bounds.X;
                double y2 = end.Y - bounds.Y;

                string strokeColor = GetColor(stroke, 1.0);
                
                _sb.AppendLine($"<div style='position: absolute; left: {left}px; top: {top}px; width: {bounds.Width}px; height: {bounds.Height}px; opacity: {opacity}; pointer-events: none;'>");
                _sb.AppendLine($"<svg width='100%' height='100%'><line x1='{x1}' y1='{y1}' x2='{x2}' y2='{y2}' stroke='{strokeColor}' stroke-width='{strokeThickness}' /></svg>");
                _sb.AppendLine("</div>");
            }

            public void DrawPath(global::Avalonia.Controls.Shapes.Path path, Rect rect, double opacity)
            {
                if (path.Data == null) return;
                
                double left = rect.X - _currentOrigin.X;
                double top = rect.Y - _currentOrigin.Y;
                
                string fill = path.Fill != null ? GetColor(path.Fill, 1.0) : "none";
                string stroke = path.Stroke != null ? GetColor(path.Stroke, 1.0) : "none";
                double strokeWidth = path.StrokeThickness;

                // Path data is local. We can use it directly in SVG.
                // But we need to handle Stretch.
                // SVG viewBox and preserveAspectRatio can handle stretch.
                
                string preserveAspectRatio = "none";
                if (path.Stretch == Stretch.None) preserveAspectRatio = "xMidYMid meet"; // Default?
                else if (path.Stretch == Stretch.Uniform) preserveAspectRatio = "xMidYMid meet";
                else if (path.Stretch == Stretch.UniformToFill) preserveAspectRatio = "xMidYMid slice";
                
                // We need the bounds of the path data to set viewBox.
                // SKPath.ParseSvgPathData(path.Data.ToString()).Bounds
                // This requires SkiaSharp.
                var skPath = SKPath.ParseSvgPathData(path.Data.ToString());
                if (skPath == null) return;
                var b = skPath.Bounds;
                
                _sb.AppendLine($"<div style='position: absolute; left: {left}px; top: {top}px; width: {rect.Width}px; height: {rect.Height}px; opacity: {opacity}; pointer-events: none;'>");
                _sb.AppendLine($"<svg width='100%' height='100%' viewBox='{b.Left} {b.Top} {b.Width} {b.Height}' preserveAspectRatio='{preserveAspectRatio}'>");
                _sb.AppendLine($"<path d='{path.Data}' fill='{fill}' stroke='{stroke}' stroke-width='{strokeWidth}' />");
                _sb.AppendLine("</svg></div>");
            }

            public void DrawText(Rect rect, string text, Control control, double opacity)
            {
                var foreground = control.GetValue(TextBlock.ForegroundProperty);
                var fontFamily = control.GetValue(TextBlock.FontFamilyProperty);
                var fontSize = control.GetValue(TextBlock.FontSizeProperty);
                var fontWeight = control.GetValue(TextBlock.FontWeightProperty);
                var fontStyle = control.GetValue(TextBlock.FontStyleProperty);
                var textAlignment = control.GetValue(TextBlock.TextAlignmentProperty);
                var verticalAlignment = control.GetValue(Layoutable.VerticalAlignmentProperty);

                string style = GetRectStyle(rect, opacity);
                style += $"color: {GetColor(foreground, 1.0)};";
                style += $"font-family: '{fontFamily?.Name ?? "Arial"}';";
                style += $"font-size: {fontSize}px;";
                style += $"font-weight: {(int)fontWeight};";
                style += $"font-style: {(fontStyle == FontStyle.Italic ? "italic" : "normal")};";
                
                // Alignment
                style += "display: flex;";
                
                if (textAlignment == TextAlignment.Center) style += "justify-content: center;";
                else if (textAlignment == TextAlignment.Right) style += "justify-content: flex-end;";
                else style += "justify-content: flex-start;";

                if (verticalAlignment == VerticalAlignment.Center) style += "align-items: center;";
                else if (verticalAlignment == VerticalAlignment.Bottom) style += "align-items: flex-end;";
                else style += "align-items: flex-start;";

                _sb.AppendLine($"<div style='{style}'>{System.Net.WebUtility.HtmlEncode(text)}</div>");
            }

            public void PushClip(Rect rect)
            {
                // Create a clipping container
                double left = rect.X - _currentOrigin.X;
                double top = rect.Y - _currentOrigin.Y;
                
                _sb.AppendLine($"<div style='position: absolute; left: {left}px; top: {top}px; width: {rect.Width}px; height: {rect.Height}px; overflow: hidden;'>");
                
                _originStack.Push(_currentOrigin);
                _currentOrigin = new Point(rect.X, rect.Y);
            }

            public void PopClip()
            {
                _sb.AppendLine("</div>");
                _currentOrigin = _originStack.Pop();
            }

            public void DrawWatermark(double canvasWidth, double canvasHeight)
            {
                string text = AppConstants.WatermarkText;
                string base64Logo = "";

                try
                {
                    var uri = new Uri("avares://SixteenCoreCharacterMapper.Avalonia/Assets/16Core%20logo%20icon%20v2.png");
                    using var stream = global::Avalonia.Platform.AssetLoader.Open(uri);
                    using var ms = new MemoryStream();
                    stream.CopyTo(ms);
                    base64Logo = Convert.ToBase64String(ms.ToArray());
                }
                catch { }

                string html = $"<div style='position: absolute; right: 10px; bottom: 10px; display: flex; align-items: center; opacity: 0.7; pointer-events: none;'>";
                html += $"<span style='font-family: Arial; font-size: 12px; color: #646464;'>{System.Net.WebUtility.HtmlEncode(text)}</span>";
                
                if (!string.IsNullOrEmpty(base64Logo))
                {
                    html += $"<img src='data:image/png;base64,{base64Logo}' style='height: 24px; margin-left: 5px;' />";
                }
                html += "</div>";

                _sb.AppendLine(html);
            }
        }
    }
}
