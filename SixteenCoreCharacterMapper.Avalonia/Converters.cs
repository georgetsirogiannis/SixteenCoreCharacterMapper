using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SixteenCoreCharacterMapper.Core.Models;

namespace SixteenCoreCharacterMapper.Avalonia
{
    public class BooleanToLockTooltipConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => (value is bool b && b) ? "Unlock Character" : "Lock Character";
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class BooleanToVisibilityTooltipConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => (value is bool b && b) ? "Hide Character" : "Show Character";
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class BubbleSizeToGroupHeaderConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is BubbleSize size ? size switch { BubbleSize.Large => "Main", BubbleSize.Medium => "Supporting", BubbleSize.Small => "Background", _ => "Other" } : "Other";
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class BooleanToOpacityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return (value is bool b && b) ? 1.0 : 0.0;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BooleanToDimOpacityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return (value is bool b && b) ? 1.0 : 0.5;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BooleanToFontWeightConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return (value is bool b && b) ? FontWeight.Bold : FontWeight.Medium;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class HexToBrushConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string hex && !string.IsNullOrEmpty(hex))
            {
                try
                {
                    return Brush.Parse(hex);
                }
                catch
                {
                    // Fallback
                }
            }
            return Brushes.Transparent;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ColorToBooleanConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count == 2 && values[0] is ColorItem colorItem && values[1] is Color selectedColor)
            {
                if (Color.TryParse(colorItem.Hex, out var itemColor))
                {
                    return itemColor == selectedColor;
                }
            }
            return false;
        }
    }

    public class StatusIconConverter : IMultiValueConverter
    {
        private static readonly Dictionary<string, Bitmap> _cache = new();

        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count < 2 || parameter is not string type) return null;

            bool status = values[0] is bool b && b;
            bool isDarkMode = values[1] is bool b2 && b2;

            string state = status ? "active" : "inactive"; // lock: active=locked, inactive=unlocked. vis: active=visible, inactive=hidden
            
            // Map status to filename part
            // Lock: IsLocked=true -> lock, IsLocked=false -> unlock
            // Vis: IsVisible=true -> eye_open, IsVisible=false -> eye_closed
            
            string iconName;
            if (type == "Lock")
            {
                iconName = status ? "lock" : "unlock";
            }
            else if (type == "Visibility")
            {
                iconName = status ? "eye_open" : "eye_closed";
            }
            else return null;

            string theme = isDarkMode ? "dark" : "light";
            string path = $"avares://SixteenCoreCharacterMapper.Avalonia/Assets/{iconName}_{theme}.png";

            if (!_cache.TryGetValue(path, out var bitmap))
            {
                try
                {
                    bitmap = new Bitmap(AssetLoader.Open(new Uri(path)));
                    _cache[path] = bitmap;
                }
                catch
                {
                    // Fallback or return null
                    return null;
                }
            }
            return bitmap;
        }
    }

    public class StatusToGeometryConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool status && parameter is string type)
            {
                if (type == "Lock")
                {
                    // Locked : Unlocked
                    return status 
                        ? StreamGeometry.Parse("M12,17A2,2 0 0,0 14,15C14,13.89 13.1,13 12,13A2,2 0 0,0 10,15A2,2 0 0,0 12,17M18,8H17V6A5,5 0 0,0 12,1A5,5 0 0,0 7,6V8H6A2,2 0 0,0 4,10V20A2,2 0 0,0 6,22H18A2,2 0 0,0 20,20V10A2,2 0 0,0 18,8M12,3A3,3 0 0,1 15,6V8H9V6A3,3 0 0,1 12,3Z") 
                        : StreamGeometry.Parse("M18,8H17V6A5,5 0 0,0 12,1C9.24,1 7,3.24 7,6H9A3,3 0 0,1 12,3A3,3 0 0,1 15,6V8H6A2,2 0 0,0 4,10V20A2,2 0 0,0 6,22H18A2,2 0 0,0 20,20V10A2,2 0 0,0 18,8M12,17A2,2 0 0,1 10,15A2,2 0 0,1 12,13A2,2 0 0,1 14,15A2,2 0 0,1 12,17Z");
                }
                else if (type == "Visibility")
                {
                    // Visible : Hidden
                    return status
                        ? StreamGeometry.Parse("M12,9A3,3 0 0,0 9,12A3,3 0 0,0 12,15A3,3 0 0,0 15,12A3,3 0 0,0 12,9M12,17A5,5 0 0,1 7,12A5,5 0 0,1 12,7A5,5 0 0,1 17,12A5,5 0 0,1 12,17M12,4.5C7,4.5 2.73,7.61 1,12C2.73,16.39 7,19.5 12,19.5C17,19.5 21.27,16.39 23,12C21.27,7.61 17,4.5 12,4.5Z")
                        : StreamGeometry.Parse("M11.83,9L15,12.16C15,12.11 15,12.05 15,12A3,3 0 0,0 12,9C11.94,9 11.89,9 11.83,9M7.53,9.8L9.08,11.35C9.03,11.56 9,11.77 9,12A3,3 0 0,0 12,15C12.22,15 12.44,14.97 12.65,14.92L14.2,16.47C13.53,16.8 12.79,17 12,17A5,5 0 0,1 7,12C7,11.21 7.2,10.47 7.53,9.8M2,4.27L4.28,6.55L4.73,7C3.08,8.3 1.78,10 1,12C2.73,16.39 7,19.5 12,19.5C13.55,19.5 15.03,19.2 16.38,18.66L16.81,19.08L19.73,22L21,20.73L3.27,3M12,7A5,5 0 0,1 17,12C17,12.64 16.87,13.26 16.64,13.82L19.57,16.75C21.07,15.5 22.27,13.86 23,12C21.27,7.61 17,4.5 12,4.5C10.6,4.5 9.27,4.73 8,5.16L10.1,7.26C10.7,7.09 11.34,7 12,7Z");
                }
            }
            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class BooleanToIconBrushConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool isDarkMode = value is bool b && b;
            // Dark Mode -> Light Gray (#D3D3D3)
            // Light Mode -> Mid Gray (#808080)
            return isDarkMode 
                ? new SolidColorBrush(Color.Parse("#D3D3D3")) 
                : new SolidColorBrush(Color.Parse("#808080"));
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
