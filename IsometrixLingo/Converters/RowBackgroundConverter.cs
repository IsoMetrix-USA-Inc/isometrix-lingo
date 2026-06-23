using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using IsometrixLingo.Models;

namespace IsometrixLingo.Converters;

/// <summary>
/// Converter that returns background color based on both HasMissingTranslations and ChangeType
/// Priority: Missing translations (red) > Modified (amber) > Added (teal) > None (transparent)
/// </summary>
public class RowBackgroundConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count >= 2 && 
            values[0] is bool hasMissingTranslations && 
            values[1] is ChangeType changeType)
        {
            // Priority 1: Missing translations (most important)
            if (hasMissingTranslations)
            {
                return new SolidColorBrush(Color.FromArgb(70, 255, 80, 80)); // Red background
            }

            // Priority 2: Change type indicators
            return changeType switch
            {
                ChangeType.Modified => new SolidColorBrush(Color.FromRgb(0xFF, 0xF3, 0xCD)), // Light amber
                ChangeType.Added => new SolidColorBrush(Color.FromRgb(0xD1, 0xEC, 0xF1)),    // Light teal
                _ => Brushes.Transparent
            };
        }

        return Brushes.Transparent;
    }
}
