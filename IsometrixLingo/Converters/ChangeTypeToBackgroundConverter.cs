using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using IsometrixLingo.Models;

namespace IsometrixLingo.Converters;

/// <summary>
/// Converter that returns background color based on ChangeType
/// Modified keys: light amber (#FFF3CD)
/// Added keys: light teal (#D1ECF1)
/// None: transparent
/// </summary>
public class ChangeTypeToBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ChangeType changeType)
        {
            return changeType switch
            {
                ChangeType.Modified => new SolidColorBrush(Color.FromRgb(0xFF, 0xF3, 0xCD)), // Light amber
                ChangeType.Added => new SolidColorBrush(Color.FromRgb(0xD1, 0xEC, 0xF1)),    // Light teal
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
