using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;
using IsometrixLingo.Models;

namespace IsometrixLingo.Converters;

/// <summary>
/// Converter that returns background color based on ChangeType
/// Modified keys: amber (theme-aware)
/// Added keys: blue (theme-aware)
/// None: transparent
/// </summary>
public class ChangeTypeToBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ChangeType changeType)
        {
            var isDarkMode = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;

            return changeType switch
            {
                ChangeType.Modified => isDarkMode
                    ? new SolidColorBrush(Color.FromArgb(100, 255, 193, 7))   // Dark mode: Brighter amber with transparency
                    : new SolidColorBrush(Color.FromRgb(255, 224, 130)),      // Light mode: Darker amber
                ChangeType.Added => isDarkMode
                    ? new SolidColorBrush(Color.FromArgb(100, 3, 169, 244))   // Dark mode: Brighter blue with transparency
                    : new SolidColorBrush(Color.FromRgb(179, 229, 252)),      // Light mode: Light blue
                _ => Brushes.Transparent
            };
        }

        return Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
