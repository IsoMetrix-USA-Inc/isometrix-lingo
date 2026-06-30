using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using IsometrixLingo.Models;

namespace IsometrixLingo.Converters;

/// <summary>
/// Returns true when a key has a change type (Modified or Added), used to show the approve action only for changed keys.
/// </summary>
public class ChangeTypeToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is ChangeType changeType && changeType != ChangeType.None;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Returns the approve button glyph: a filled check when approved, an outline check when not.
/// </summary>
public class ApproveButtonContentConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool approved && approved ? "\u2705" : "\u2713";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Returns the approve button foreground: green when approved, default otherwise.
/// </summary>
public class ApproveButtonForegroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool approved && approved
            ? new SolidColorBrush(Color.FromRgb(46, 125, 50))
            : Brushes.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Returns the approve button tooltip text based on approval state.
/// </summary>
public class ApproveButtonTooltipConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool approved && approved
            ? "Reviewed and approved - click to un-approve"
            : "Mark this change as reviewed and approved";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
