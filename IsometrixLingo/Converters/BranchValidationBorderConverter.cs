using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace IsometrixLingo.Converters;

/// <summary>
/// Returns a border brush for a branch input based on validation state.
/// Inputs: [0] isValid (bool), [1] hasError (bool).
/// Green when valid, red when in error, neutral gray otherwise.
/// </summary>
public class BranchValidationBorderConverter : IMultiValueConverter
{
    private static readonly SolidColorBrush Valid = new(Color.FromRgb(46, 125, 50));   // green
    private static readonly SolidColorBrush Error = new(Color.FromRgb(198, 40, 40));    // red
    private static readonly SolidColorBrush Neutral = new(Color.FromRgb(170, 170, 170)); // gray

    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var isValid = values.Count > 0 && values[0] is bool v && v;
        var hasError = values.Count > 1 && values[1] is bool e && e;

        if (hasError)
            return Error;
        if (isValid)
            return Valid;
        return Neutral;
    }
}
