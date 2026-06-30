using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;
using IsometrixLingo.Models;

namespace IsometrixLingo.Converters;

/// <summary>
/// Converter that returns background color based on both HasMissingTranslations and ChangeType
/// Priority: Missing translations (red) > Modified (amber) > Added (teal) > None (transparent)
/// Theme-aware: Uses different colors for light and dark modes
/// </summary>
public class RowBackgroundConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count >= 2 &&
            values[0] is bool hasMissingTranslations &&
            values[1] is ChangeType changeType)
        {
            var isDarkMode = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
            var isApproved = values.Count >= 3 && values[2] is bool approved && approved;

            // Priority 1: Missing translations (most important)
            if (hasMissingTranslations)
            {
                return new SolidColorBrush(Color.FromArgb(70, 255, 80, 80)); // Red background (same for both themes)
            }

            // Priority 2: Approved/reviewed changes (green) - takes precedence over change type
            if (isApproved && changeType != ChangeType.None)
            {
                return isDarkMode
                    ? new SolidColorBrush(Color.FromArgb(100, 76, 175, 80))   // Dark mode: Brighter green with transparency
                    : new SolidColorBrush(Color.FromRgb(200, 230, 201));      // Light mode: Soft green
            }

            // Priority 3: Change type indicators (theme-aware)
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
}
