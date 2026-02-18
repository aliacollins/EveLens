using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace EVEMon.Avalonia.Converters
{
    /// <summary>
    /// Converts a boolean to FontWeight: true = Bold, false = Normal.
    /// </summary>
    public sealed class BoolToFontWeightConverter : IValueConverter
    {
        public static readonly BoolToFontWeightConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is true)
                return FontWeight.Bold;

            return FontWeight.Normal;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("BoolToFontWeightConverter is one-way only.");
        }
    }
}
